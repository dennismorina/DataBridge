CREATE TABLE products
(
    id bigserial PRIMARY KEY,
    sku varchar(50) NOT NULL,
    name varchar(200) NOT NULL,
    price numeric(18,2) NOT NULL CHECK (price >= 0),
    stock_quantity integer NOT NULL CHECK (stock_quantity >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_products_sku UNIQUE (sku)
);

CREATE TABLE import_jobs
(
    id uuid PRIMARY KEY,
    file_name text NOT NULL,
    source_hash char(64) NOT NULL,
    status varchar(20) NOT NULL,
    total_rows integer NOT NULL,
    accepted_rows integer NOT NULL,
    rejected_rows integer NOT NULL,
    upserted_rows integer NOT NULL,
    duration_ms bigint NOT NULL,
    started_at timestamptz NOT NULL,
    completed_at timestamptz NOT NULL,
    error_message text NULL
);

CREATE INDEX ix_import_jobs_source_hash_status
    ON import_jobs (source_hash, status);
