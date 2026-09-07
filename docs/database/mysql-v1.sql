-- AI Trading Platform V1 - MySQL schema foundation
-- Execution is paper-only. No broker order table is enabled for live execution.
CREATE DATABASE IF NOT EXISTS ai_trading CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE ai_trading;

CREATE TABLE IF NOT EXISTS instruments (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    symbol VARCHAR(64) NOT NULL,
    exchange VARCHAR(16) NOT NULL,
    instrument_token VARCHAR(64) NOT NULL,
    trading_symbol VARCHAR(128) NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE KEY uq_instrument (exchange, instrument_token),
    UNIQUE KEY uq_symbol_exchange (exchange, symbol)
);

CREATE TABLE IF NOT EXISTS market_candles (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    instrument_id BIGINT NOT NULL,
    timestamp_utc DATETIME(6) NOT NULL,
    open_price DECIMAL(20,8) NOT NULL,
    high_price DECIMAL(20,8) NOT NULL,
    low_price DECIMAL(20,8) NOT NULL,
    close_price DECIMAL(20,8) NOT NULL,
    volume BIGINT NOT NULL,
    UNIQUE KEY uq_candle (instrument_id, timestamp_utc),
    CONSTRAINT fk_candle_instrument FOREIGN KEY (instrument_id) REFERENCES instruments(id)
);

CREATE TABLE IF NOT EXISTS paper_orders (
    id CHAR(36) PRIMARY KEY,
    symbol VARCHAR(64) NOT NULL,
    side VARCHAR(8) NOT NULL,
    quantity INT NOT NULL,
    limit_price DECIMAL(20,8) NOT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    status VARCHAR(16) NOT NULL DEFAULT 'FILLED'
);

CREATE TABLE IF NOT EXISTS paper_fills (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id CHAR(36) NOT NULL,
    symbol VARCHAR(64) NOT NULL,
    side VARCHAR(8) NOT NULL,
    quantity INT NOT NULL,
    price DECIMAL(20,8) NOT NULL,
    timestamp_utc DATETIME(6) NOT NULL,
    source VARCHAR(32) NOT NULL,
    CONSTRAINT fk_fill_order FOREIGN KEY (order_id) REFERENCES paper_orders(id)
);

CREATE TABLE IF NOT EXISTS alerts (
    alert_key VARCHAR(255) PRIMARY KEY,
    severity VARCHAR(16) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    symbol VARCHAR(64) NULL,
    created_at_utc DATETIME(6) NOT NULL
);

CREATE INDEX ix_market_candles_timestamp ON market_candles(timestamp_utc);
CREATE INDEX ix_alerts_created ON alerts(created_at_utc);
