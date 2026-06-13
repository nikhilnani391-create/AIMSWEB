package queue

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"log"
	"math"
	"net/http"
	"sync"
	"time"

	"github.com/google/uuid"
	"github.com/redis/go-redis/v9"
)

const (
	queueKey           = "matchmaking:queue"
	playerDataPrefix   = "matchmaking:player:"
	matchSessionPrefix = "matchmaking:session:"

	matchSize             = 50
	baseToleranceElo      = 50.0
	toleranceExpandPerSec = 5.0
	maxTolerance          = 500.0
	tickInterval          = 2 * time.Second
)

type PlayerTicket struct {
	PlayerID   string  `json:"playerId"`
	RankPoints float64 `json:"rankPoints"`
	EnqueuedAt int64   `json:"enqueuedAt"`
}

type MatchSession struct {
	MatchID   string         `json:"matchId"`
	Players   []PlayerTicket `json:"players"`
	CreatedAt int64          `json:"createdAt"`
	ServerIP  string         `json:"serverIp,omitempty"`
	Port      int            `json:"port,omitempty"`
	Status    string         `json:"status"`
}

type MatchmakingQueue struct {
	rdb             *redis.Client
	orchestratorURL string
	mu              sync.RWMutex
	activeSessions  map[string]*MatchSession
}

func NewMatchmakingQueue(rdb *redis.Client, orchestratorURL string) *MatchmakingQueue {
	return &MatchmakingQueue{
		rdb:             rdb,
		orchestratorURL: orchestratorURL,
		activeSessions:  make(map[string]*MatchSession),
	}
}

func (mq *MatchmakingQueue) EnqueuePlayer(ctx context.Context, ticket PlayerTicket) error {
	ticket.EnqueuedAt = time.Now().Unix()

	data, err := json.Marshal(ticket)
	if err != nil {
		return fmt.Errorf("failed to marshal ticket: %w", err)
	}

	pipe := mq.rdb.Pipeline()
	pipe.ZAdd(ctx, queueKey, redis.Z{
		Score:  ticket.RankPoints,
		Member: ticket.PlayerID,
	})
	pipe.Set(ctx, playerDataPrefix+ticket.PlayerID, data, 10*time.Minute)
	_, err = pipe.Exec(ctx)

	return err
}

func (mq *MatchmakingQueue) DequeuePlayer(ctx context.Context, playerID string) error {
	pipe := mq.rdb.Pipeline()
	pipe.ZRem(ctx, queueKey, playerID)
	pipe.Del(ctx, playerDataPrefix+playerID)
	_, err := pipe.Exec(ctx)
	return err
}

func (mq *MatchmakingQueue) GetPlayerStatus(ctx context.Context, playerID string) (string, error) {
	exists, err := mq.rdb.ZScore(ctx, queueKey, playerID).Result()
	if err == redis.Nil {
		mq.mu.RLock()
		defer mq.mu.RUnlock()
		for _, session := range mq.activeSessions {
			for _, p := range session.Players {
				if p.PlayerID == playerID {
					return fmt.Sprintf("matched:%s", session.MatchID), nil
				}
			}
		}
		return "not_in_queue", nil
	}
	if err != nil {
		return "", err
	}
	_ = exists
	return "in_queue", nil
}

func (mq *MatchmakingQueue) RunMatchmakingLoop(ctx context.Context) {
	ticker := time.NewTicker(tickInterval)
	defer ticker.Stop()

	log.Println("[matchmaker] Matchmaking loop started.")

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			mq.tryFormMatch(ctx)
		}
	}
}

func (mq *MatchmakingQueue) tryFormMatch(ctx context.Context) {
	queueSize, err := mq.rdb.ZCard(ctx, queueKey).Result()
	if err != nil {
		log.Printf("[matchmaker] Error checking queue size: %v", err)
		return
	}

	if queueSize < int64(matchSize/2) {
		return
	}

	members, err := mq.rdb.ZRangeWithScores(ctx, queueKey, 0, -1).Result()
	if err != nil {
		log.Printf("[matchmaker] Error reading queue: %v", err)
		return
	}

	tickets := make([]PlayerTicket, 0, len(members))
	for _, m := range members {
		playerID := m.Member.(string)
		data, err := mq.rdb.Get(ctx, playerDataPrefix+playerID).Result()
		if err != nil {
			continue
		}

		var ticket PlayerTicket
		if err := json.Unmarshal([]byte(data), &ticket); err != nil {
			continue
		}
		tickets = append(tickets, ticket)
	}

	if len(tickets) < matchSize/2 {
		return
	}

	now := time.Now().Unix()
	matched := make([]PlayerTicket, 0, matchSize)

	for i := 0; i < len(tickets) && len(matched) < matchSize; i++ {
		anchor := tickets[i]
		waitTime := float64(now - anchor.EnqueuedAt)
		tolerance := math.Min(baseToleranceElo+toleranceExpandPerSec*waitTime, maxTolerance)

		if len(matched) == 0 {
			matched = append(matched, anchor)
			continue
		}

		avgRank := averageRank(matched)
		if math.Abs(anchor.RankPoints-avgRank) <= tolerance {
			matched = append(matched, anchor)
		}
	}

	if len(matched) < matchSize/2 {
		return
	}

	matchID := uuid.New().String()
	session := &MatchSession{
		MatchID:   matchID,
		Players:   matched,
		CreatedAt: now,
		Status:    "forming",
	}

	pipe := mq.rdb.Pipeline()
	for _, p := range matched {
		pipe.ZRem(ctx, queueKey, p.PlayerID)
		pipe.Del(ctx, playerDataPrefix+p.PlayerID)
	}
	if _, err := pipe.Exec(ctx); err != nil {
		log.Printf("[matchmaker] Error removing matched players: %v", err)
		return
	}

	mq.mu.Lock()
	mq.activeSessions[matchID] = session
	mq.mu.Unlock()

	sessionData, _ := json.Marshal(session)
	mq.rdb.Set(ctx, matchSessionPrefix+matchID, sessionData, 30*time.Minute)

	log.Printf("[matchmaker] Match formed: %s with %d players", matchID, len(matched))

	go mq.requestServerAllocation(matchID, session)
}

func (mq *MatchmakingQueue) requestServerAllocation(matchID string, session *MatchSession) {
	payload, _ := json.Marshal(map[string]interface{}{
		"matchId": matchID,
		"players": session.Players,
	})

	url := fmt.Sprintf("%s/api/orchestrator/allocate", mq.orchestratorURL)
	resp, err := http.Post(url, "application/json", bytes.NewBuffer(payload))
	if err != nil {
		log.Printf("[matchmaker] Failed to request server for match %s: %v", matchID, err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusOK {
		var result struct {
			ServerIP string `json:"serverIp"`
			Port     int    `json:"port"`
		}
		if err := json.NewDecoder(resp.Body).Decode(&result); err == nil {
			mq.mu.Lock()
			session.ServerIP = result.ServerIP
			session.Port = result.Port
			session.Status = "server_allocated"
			mq.mu.Unlock()

			log.Printf("[matchmaker] Server allocated for match %s at %s:%d", matchID, result.ServerIP, result.Port)
		}
	}
}

func averageRank(players []PlayerTicket) float64 {
	if len(players) == 0 {
		return 0
	}
	sum := 0.0
	for _, p := range players {
		sum += p.RankPoints
	}
	return sum / float64(len(players))
}
