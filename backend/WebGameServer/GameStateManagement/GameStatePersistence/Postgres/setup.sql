CREATE TABLE IF NOT EXISTS GAME_INFO (
   game_id INTEGER PRIMARY KEY,
   game_info_json JSONB,
   created_at TIMESTAMP DEFAULT now()
);

-- Ensure index exists for fast lookups
CREATE INDEX IF NOT EXISTS idx_game_id ON GAME_INFO(game_id);