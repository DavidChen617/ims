create table stocks (
    id uuid primary key,
    product_id uuid not null,
    warehouse_id uuid not null,
    quantity integer not null,
    cumulative_shipped integer not null,
    unique (product_id, warehouse_id)
);
