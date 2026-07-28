create table inbox_messages (
    event_id uuid primary key,
    processed_at timestamptz not null
);
