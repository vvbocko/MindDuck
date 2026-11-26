BEGIN;

CREATE TEMP TABLE import_cards ON COMMIT DROP AS
SELECT
    card->>'id' AS id,
    card->>'name' AS name,
    (card->>'power')::integer AS power,
    card->>'keyword' AS keyword,
    card->>'artKey' AS art_key,
    card->'abilities' AS abilities
FROM jsonb_array_elements(pg_read_file('/tmp/cards.json')::jsonb) AS entries(card);

INSERT INTO cards (id, name, power, keyword, art_key)
SELECT id, name, power, keyword, art_key
FROM import_cards
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    power = EXCLUDED.power,
    keyword = EXCLUDED.keyword,
    art_key = EXCLUDED.art_key;

DELETE FROM card_abilities AS existing
USING import_cards AS imported
WHERE existing.card_id = imported.id;

INSERT INTO card_abilities (card_id, trigger, effect, value)
SELECT cards.id, ability.trigger, ability.effect, ability.value
FROM import_cards AS cards
CROSS JOIN LATERAL jsonb_to_recordset(cards.abilities)
    AS ability(trigger text, effect text, value integer);

COMMIT;