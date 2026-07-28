create table refresh_token (
    id uuid primary key,
    token text not null unique,
    replaced_by_token text null,
    user_id uuid not null references users (id),
    created_at timestamptz not null,
    expires_at timestamptz not null,
    revoke_at timestamptz null
);

create index ix_refresh_token_user_id on refresh_token (user_id);
