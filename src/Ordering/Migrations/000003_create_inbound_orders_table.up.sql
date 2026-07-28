create table inbound_orders (
    id uuid primary key,
    order_no text not null unique,
    warehouse_id uuid not null,
    status smallint not null,
    reject_reason text null,
    requested_by uuid not null,
    requested_at timestamptz not null,
    confirmed_by uuid null,
    confirmed_at timestamptz null
);

create table inbound_order_items (
    id serial primary key,
    inbound_order_id uuid not null references inbound_orders (id),
    product_id uuid not null references products (id),
    quantity integer not null,
    unit_price numeric not null
);

create index ix_inbound_order_items_inbound_order_id on inbound_order_items (inbound_order_id);
