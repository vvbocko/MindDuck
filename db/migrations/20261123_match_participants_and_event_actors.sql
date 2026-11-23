BEGIN;

ALTER TABLE matches
    ADD COLUMN IF NOT EXISTS player1_id integer REFERENCES players (id),
    ADD COLUMN IF NOT EXISTS player2_id integer REFERENCES players (id);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'matches_distinct_players_check'
          AND conrelid = 'matches'::regclass
    ) THEN
        ALTER TABLE matches
            ADD CONSTRAINT matches_distinct_players_check
            CHECK (
                player1_id IS NULL
                OR player2_id IS NULL
                OR player1_id <> player2_id
            );
    END IF;
END
$$;

ALTER TABLE match_events
    ADD COLUMN IF NOT EXISTS actor_player_id integer REFERENCES players (id);

CREATE INDEX IF NOT EXISTS matches_player1_id_idx ON matches (player1_id);
CREATE INDEX IF NOT EXISTS matches_player2_id_idx ON matches (player2_id);
CREATE INDEX IF NOT EXISTS match_events_actor_player_id_idx
    ON match_events (actor_player_id);

COMMIT;
