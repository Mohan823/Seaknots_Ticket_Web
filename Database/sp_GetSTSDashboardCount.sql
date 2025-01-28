
ALTER PROCEDURE sp_GetSTSDashboardCount
(
	@Type VARCHAR(MAX)
)
AS BEGIN
SET NOCOUNT ON

DECLARE @SQL NVARCHAR(MAX);
CREATE TABLE #TEMP(userName VARCHAR(255), OrderId INT)

IF(@Type = 'AssignedEmployee')
BEGIN
	INSERT INTO #TEMP VALUES
	('kuppu', 1),
	('dhana', 2),
	('HDhana', 3),
	('Prabu', 4),
	('aswanth', 5),
	('AswanthSK', 6),
	('Mohan', 7),
	('HMohan', 8),
	('jagathesan', 9),
	('Vignesh', 10),
	('TGLVicky', 11),
	('guru', 12),
	('IBGURU', 13),
	('kamesh', 14),
	('NWKamesh', 15),
	('marimuthu', 16),
	('NWMuthu', 17),
	('mukesh', 18),
	('Chandru', 19),
	('abhishek', 20),
	('Joshua', 21),
	('IBJoshua', 22),
	('TGLJoshua', 23),
	('VijayaRagavan', 24),
	('NWVijay', 25),
	('Arjun', 26),
	('Vasanth', 27),
	('Bharath', 28),
	('NWBharath', 29),
	('Natheeshkumar', 30),
	('Kalai', 31),
	('NandhaKumar', 32),
	('Megaraj', 33)

	SELECT A.AssignedEmployee , COUNT(*) Count 
	FROM Jiras A
	INNER JOIN #TEMP B ON B.userName = A.AssignedEmployee
	WHERE 
		IsActive = 1 
		AND Status IN ('TO DO', 'RESOLVED', 'IN REVIEW', 'Sent for Approval', 'REOPENED', 'IN PROGRESS')
		GROUP BY A.AssignedEmployee, B.OrderId 
		ORDER BY B.OrderId ASC

END
ELSE BEGIN
	SET @SQL ='
	SELECT ' + QUOTENAME(@Type) + ' , COUNT(*) Count 
	FROM Jiras 
	WHERE 
		IsActive = 1 
		AND Status IN (''TO DO'', ''RESOLVED'', ''IN REVIEW'', ''Sent for Approval'', ''REOPENED'', ''IN PROGRESS'')
		GROUP BY ' + QUOTENAME(@Type)

	EXEC sp_executesql @SQL;
END
DROP TABLE #TEMP;
END
GO
sp_GetSTSDashboardCount 'Project'