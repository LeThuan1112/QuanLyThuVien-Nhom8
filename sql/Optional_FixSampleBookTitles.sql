/* Optional data cleanup: correct two exact sample-title typos.
   Review the SELECT results before running the UPDATE statements.
   Run only against the intended database after keeping a backup. */
USE [ThuVienDb];
GO

SELECT BookId, BookCode, Title
FROM dbo.Books
WHERE Title IN (N'ĐÔ Rê Mon', N'Shin câu bé bút chì');
GO

BEGIN TRANSACTION;

UPDATE dbo.Books
SET Title = N'Doraemon'
WHERE Title = N'ĐÔ Rê Mon';

UPDATE dbo.Books
SET Title = N'Shin – Cậu bé bút chì'
WHERE Title = N'Shin câu bé bút chì';

SELECT BookId, BookCode, Title
FROM dbo.Books
WHERE Title IN (N'Doraemon', N'Shin – Cậu bé bút chì');

COMMIT TRANSACTION;
GO
