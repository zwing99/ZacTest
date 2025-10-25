-- init-db.sql
-- This script will be executed by the official Postgres image on container initialization

-- Create a simple users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

-- Create a simple products table
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    price NUMERIC(10,2) DEFAULT 0.00,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

-- Insert some test users
INSERT INTO users (username, email) VALUES
  ('alice', 'alice@example.com'),
  ('bob', 'bob@example.com')
ON CONFLICT DO NOTHING;

-- Insert some test products
INSERT INTO products (name, description, price) VALUES
  ('Widget', 'A useful widget', 9.99),
  ('Gadget', 'A fancy gadget', 19.95)
ON CONFLICT DO NOTHING;
