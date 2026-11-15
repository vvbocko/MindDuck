CREATE TABLE IF NOT EXISTS cards (
    id VARCHAR(255) PRIMARY KEY,
	name VARCHAR(255) NOT NULL,
	power INT NOT NULL,
	keyword VARCHAR(255) NULL
        CONSTRAINT ck_cards_keyword CHECK (keyword IN ('Poisonous', 'Tough', 'Frenzy')),
	art_key VARCHAR(255)

);

CREATE TABLE IF NOT EXISTS card_abilities (
    id SERIAL PRIMARY KEY,
    card_id VARCHAR(255) NOT NULL REFERENCES cards (id) ON DELETE CASCADE,
    trigger VARCHAR(255) NOT NULL
        CONSTRAINT ck_card_abilities_trigger CHECK (trigger IN ('Play', 'Attack', 'Defeated')),
    effect VARCHAR(255) NOT NULL
        CONSTRAINT ck_card_abilities_effect CHECK (effect IN ('GainLife', 'DefeatEnemyCreature', 'OpponentDiscards')),
    value INT NULL
);

CREATE TABLE IF NOT EXISTS players (
    id integer PRIMARY KEY,
    name text NOT NULL,
    wins integer NOT NULL DEFAULT 0 CHECK (wins >= 0),
    losses integer NOT NULL DEFAULT 0 CHECK (losses >= 0)
);

CREATE TABLE IF NOT EXISTS matches (
    match_id text PRIMARY KEY,
    winner text,
    started_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    finished_at timestamptz
);

CREATE TABLE IF NOT EXISTS match_events (
    event_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    match_id text NOT NULL REFERENCES matches (match_id) ON DELETE CASCADE,
    event text NOT NULL
);

CREATE INDEX IF NOT EXISTS matches_finished_at_idx
    ON matches (finished_at DESC, match_id)
    WHERE finished_at IS NOT NULL;