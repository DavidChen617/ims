alter table inbound_orders
    drop column requested_by_name,
    drop column confirmed_by_name;

alter table outbound_orders
    drop column requested_by_name,
    drop column confirmed_by_name;
