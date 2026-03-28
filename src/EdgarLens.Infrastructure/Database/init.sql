CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS filings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticker VARCHAR(10) NOT NULL,
    company_name VARCHAR(255) NOT NULL,
    cik VARCHAR(20) NOT NULL,
    accession_number VARCHAR(50) NOT NULL UNIQUE,
    filing_date DATE NOT NULL,
    raw_content TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_filings_ticker ON filings(ticker);
CREATE INDEX IF NOT EXISTS idx_filings_accession ON filings(accession_number);

CREATE TABLE IF NOT EXISTS filing_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filing_id UUID NOT NULL REFERENCES filings(id) ON DELETE CASCADE,
    ticker VARCHAR(10) NOT NULL,
    chunk_index INT NOT NULL,
    content TEXT NOT NULL,
    embedding vector(768),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chunks_filing_id ON filing_chunks(filing_id);
CREATE INDEX IF NOT EXISTS idx_chunks_ticker ON filing_chunks(ticker);