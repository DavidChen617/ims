#!/usr/bin/env bash
migrate -path . -database "postgres://postgres:password@127.0.0.1:5432/ordering_db?sslmode=disable" "$@"
