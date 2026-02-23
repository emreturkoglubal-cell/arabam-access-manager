# RAG (Vektör) index – pgvector

AI sohbetinde daha isabetli cevaplar için kod tabanı **vektör araması** kullanılır. Soru metni embed edilir, PostgreSQL'deki `code_chunks` tablosunda benzerlik araması yapılır, en alakalı parçalar modele context olarak verilir.

## Kurulum

1. **PostgreSQL'de pgvector extension**
   - Sunucuda bir kez: `CREATE EXTENSION IF NOT EXISTS vector;`
   - Gerekirse DB yetkisi: `grant execute on schema public to ...;` vb.

2. **Tablo**
   - `database/04_code_chunks_pgvector.sql` dosyasını mevcut veritabanında çalıştırın (extension + `code_chunks` tablosu + HNSW indeksi).

3. **Config**
   - `ConnectionStrings:DefaultConnection` zaten var (PostgreSQL).
   - `OpenAI:ApiKey` chat için kullanılıyor; embedding için de aynı anahtar kullanılır.
   - İsteğe bağlı: `OpenAI:EmbeddingModel` (varsayılan: `text-embedding-3-small`).

4. **İlk index**
   - Admin kullanıcı ile giriş yapın.
   - `POST /Ai/Reindex` çağrısı yapın (ör. tarayıcıdan bir buton veya curl/Postman).
   - İşlem repo'yu tarar, .cs/.cshtml/.json dosyalarını embed edip `code_chunks` tablosuna yazar.

Bundan sonra her AI sorusunda önce soru embed edilir, vektör araması ile en alakalı 10 parça bulunur ve system prompt'a eklenir; model cevabı önce bu parçalara dayandırır.

## Reindex ne zaman?

- Repo'da önemli kod değişiklikleri sonrası.
- İsteğe bağlı: CI/CD veya periyodik job ile `POST /Ai/Reindex` tetiklenebilir.
