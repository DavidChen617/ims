create table outbox_messages (
    id uuid primary key,
    event_type text not null,
    payload text not null,
    occurred_on timestamptz not null,
    processed_on timestamptz null,
    error text null
);

create index ix_outbox_messages_processed_on on outbox_messages (processed_on) where processed_on is null;
