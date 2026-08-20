SET XACT_ABORT ON;
BEGIN TRANSACTION;

MERGE dbo.Plans AS target
USING
(
    VALUES
        (N'51d0174da64c428b9195b9a9609573af', N'gratuit', N'Gratu' + NCHAR(239) + N't', CAST(1073741824 AS bigint), 1, 2147483647, CAST(0.00 AS decimal(10, 2)), CAST(1 AS bit)),
        (N'25279403341f44ec92db8b7b4f8797a8', N'senzill', N'Senzill', CAST(5368709120 AS bigint), 3, 2147483647, CAST(5.00 AS decimal(10, 2)), CAST(1 AS bit)),
        (N'343c5d6da01f470c87341ef874629674', N'pro', N'Pro', CAST(16106127360 AS bigint), 10, 2147483647, CAST(10.00 AS decimal(10, 2)), CAST(1 AS bit))
) AS source (PlaPK, OldPlaPK, Nom, LimitEmmagatzematgeBytes, LimitSales, LimitImatges, Preu, Actiu)
ON target.PlaPK = source.PlaPK
WHEN MATCHED THEN
    UPDATE SET
        Nom = source.Nom,
        LimitEmmagatzematgeBytes = source.LimitEmmagatzematgeBytes,
        LimitSales = source.LimitSales,
        LimitImatges = source.LimitImatges,
        Preu = source.Preu,
        Actiu = source.Actiu
WHEN NOT MATCHED BY TARGET THEN
    INSERT (PlaPK, Nom, LimitEmmagatzematgeBytes, LimitSales, LimitImatges, Preu, Actiu)
    VALUES (source.PlaPK, source.Nom, source.LimitEmmagatzematgeBytes, source.LimitSales, source.LimitImatges, source.Preu, source.Actiu);

UPDATE usuariPla
SET PlaPK = source.PlaPK
FROM dbo.UsuariPlans AS usuariPla
INNER JOIN
(
    VALUES
        (N'51d0174da64c428b9195b9a9609573af', N'gratuit'),
        (N'25279403341f44ec92db8b7b4f8797a8', N'senzill'),
        (N'343c5d6da01f470c87341ef874629674', N'pro')
) AS source (PlaPK, OldPlaPK)
    ON usuariPla.PlaPK = source.OldPlaPK;

DELETE plans
FROM dbo.Plans AS plans
WHERE plans.PlaPK IN (N'gratuit', N'senzill', N'pro')
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuariPlans AS usuariPla
      WHERE usuariPla.PlaPK = plans.PlaPK
  );

COMMIT TRANSACTION;

SELECT
    PlaPK,
    Nom,
    LimitEmmagatzematgeBytes,
    LimitSales,
    LimitImatges,
    Preu,
    Actiu
FROM dbo.Plans
WHERE PlaPK IN
(
    N'51d0174da64c428b9195b9a9609573af',
    N'25279403341f44ec92db8b7b4f8797a8',
    N'343c5d6da01f470c87341ef874629674'
)
ORDER BY
    CASE PlaPK
        WHEN N'51d0174da64c428b9195b9a9609573af' THEN 1
        WHEN N'25279403341f44ec92db8b7b4f8797a8' THEN 2
        WHEN N'343c5d6da01f470c87341ef874629674' THEN 3
    END;
