alter table outbox_messages
    drop column retry_count,
    drop column dead_lettered_at;
