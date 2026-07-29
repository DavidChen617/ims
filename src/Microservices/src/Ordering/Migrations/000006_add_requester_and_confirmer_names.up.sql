alter table inbound_orders
    add column requested_by_name text,
    add column confirmed_by_name text;

alter table outbound_orders
    add column requested_by_name text,
    add column confirmed_by_name text;
