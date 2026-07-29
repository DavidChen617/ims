create table products (
    id uuid primary key,
    product_no text not null unique,
    name text not null,
    unit text not null references product_units (name),
    price numeric not null
);
