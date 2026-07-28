create table outbound_orders (
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

create table outbound_order_items (
    id serial primary key,
    outbound_order_id uuid not null references outbound_orders (id),
    product_id uuid not null references products (id),
    quantity integer not null
);

create index ix_outbound_order_items_outbound_order_id on outbound_order_items (outbound_order_id);
