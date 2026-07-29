alter table outbox_messages
    add column retry_count int not null default 0,
    add column dead_lettered_at timestamptz null;
