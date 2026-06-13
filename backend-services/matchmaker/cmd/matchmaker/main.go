package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/nikhilnani391-create/AIMSWEB/matchmaker/internal/handlers"
	"github.com/nikhilnani391-create/AIMSWEB/matchmaker/internal/queue"
	"github.com/redis/go-redis/v9"
)

func main() {
	redisAddr := getEnv("REDIS_ADDR", "redis:6379")
	redisPassword := getEnv("REDIS_PASSWORD", "")
	port := getEnv("PORT", "4000")
	orchestratorURL := getEnv("ORCHESTRATOR_URL", "http://orchestrator:5000")

	rdb := redis.NewClient(&redis.Options{
		Addr:         redisAddr,
		Password:     redisPassword,
		DB:           0,
		PoolSize:     20,
		MinIdleConns: 5,
		DialTimeout:  5 * time.Second,
		ReadTimeout:  3 * time.Second,
		WriteTimeout: 3 * time.Second,
	})

	ctx := context.Background()
	if err := rdb.Ping(ctx).Err(); err != nil {
		log.Fatalf("[matchmaker] Failed to connect to Redis: %v", err)
	}
	log.Println("[matchmaker] Redis connected.")

	matchQueue := queue.NewMatchmakingQueue(rdb, orchestratorURL)

	go matchQueue.RunMatchmakingLoop(ctx)

	r := gin.Default()
	h := handlers.NewHandler(matchQueue)

	r.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{
			"status":  "ok",
			"service": "matchmaker",
		})
	})

	r.POST("/api/matchmaking/enqueue", h.Enqueue)
	r.DELETE("/api/matchmaking/dequeue", h.Dequeue)
	r.GET("/api/matchmaking/status/:playerId", h.Status)

	srv := &http.Server{
		Addr:         fmt.Sprintf(":%s", port),
		Handler:      r,
		ReadTimeout:  10 * time.Second,
		WriteTimeout: 10 * time.Second,
	}

	go func() {
		log.Printf("[matchmaker] Listening on port %s", port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("[matchmaker] Server error: %v", err)
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit
	log.Println("[matchmaker] Shutting down...")

	shutdownCtx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	if err := srv.Shutdown(shutdownCtx); err != nil {
		log.Printf("[matchmaker] Shutdown error: %v", err)
	}

	rdb.Close()
	log.Println("[matchmaker] Shut down complete.")
}

func getEnv(key, fallback string) string {
	if v, ok := os.LookupEnv(key); ok {
		return v
	}
	return fallback
}
