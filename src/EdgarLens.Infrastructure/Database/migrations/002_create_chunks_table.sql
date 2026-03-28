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
CREATE INDEX IF NOT EXISTS idx_chunks_embedding ON filing_chunks USING ivfflat (embedding vector_cosine_ops);