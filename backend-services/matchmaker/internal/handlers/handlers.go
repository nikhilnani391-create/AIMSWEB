package handlers

import (
	"net/http"

	"github.com/gin-gonic/gin"
	"github.com/nikhilnani391-create/AIMSWEB/matchmaker/internal/queue"
)

type Handler struct {
	matchQueue *queue.MatchmakingQueue
}

func NewHandler(mq *queue.MatchmakingQueue) *Handler {
	return &Handler{matchQueue: mq}
}

type EnqueueRequest struct {
	PlayerID   string  `json:"playerId" binding:"required"`
	RankPoints float64 `json:"rankPoints" binding:"required"`
}

func (h *Handler) Enqueue(c *gin.Context) {
	var req EnqueueRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "playerId and rankPoints are required"})
		return
	}

	ticket := queue.PlayerTicket{
		PlayerID:   req.PlayerID,
		RankPoints: req.RankPoints,
	}

	if err := h.matchQueue.EnqueuePlayer(c.Request.Context(), ticket); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to enqueue player"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"status":   "queued",
		"playerId": req.PlayerID,
	})
}

type DequeueRequest struct {
	PlayerID string `json:"playerId" binding:"required"`
}

func (h *Handler) Dequeue(c *gin.Context) {
	var req DequeueRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "playerId is required"})
		return
	}

	if err := h.matchQueue.DequeuePlayer(c.Request.Context(), req.PlayerID); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to dequeue player"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"status":   "removed",
		"playerId": req.PlayerID,
	})
}

func (h *Handler) Status(c *gin.Context) {
	playerID := c.Param("playerId")
	if playerID == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "playerId is required"})
		return
	}

	status, err := h.matchQueue.GetPlayerStatus(c.Request.Context(), playerID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get player status"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"playerId": playerID,
		"status":   status,
	})
}
