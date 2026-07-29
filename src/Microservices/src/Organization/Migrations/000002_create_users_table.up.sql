create table users (
    id uuid primary key,
    warehouse_id uuid null references warehouse (id),
    name text not null,
    username text not null unique,
    password_hash text not null,
    created_at timestamptz not null,
    role smallint not null
);

create index ix_users_warehouse_id on users (warehouse_id);
