DECLARE
    @IdModelo UNIQUEIDENTIFIER,
    @IdVersionAnterior UNIQUEIDENTIFIER,
    @IdVersionNueva UNIQUEIDENTIFIER =
        '11111111-1111-1111-1111-111111111113',
    @IdFactorRci UNIQUEIDENTIFIER =
        '20000000-0000-0000-0000-000000000009';


/* ============================================================
   1. Obtener modelo
   ============================================================ */

SELECT
    @IdModelo = IdModelo
FROM modelos_scoring
WHERE Codigo = 'MODELO_PERSONAL';


IF @IdModelo IS NULL
BEGIN
    THROW 50001,
        'No existe el modelo MODELO_PERSONAL.',
        1;
END;


/* ============================================================
   2. Obtener versión 1.0.0
   ============================================================ */

SELECT
    @IdVersionAnterior = IdVersionModelo
FROM versiones_modelo
WHERE IdModelo = @IdModelo
  AND NumeroVersion = '1.0.0';


IF @IdVersionAnterior IS NULL
BEGIN
    THROW 50002,
        'No existe la versión 1.0.0 del modelo.',
        1;
END;


/* ============================================================
   3. Crear versión 1.1.0 si todavía no existe
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM versiones_modelo
    WHERE IdModelo = @IdModelo
      AND NumeroVersion = '1.1.0'
)
BEGIN

    /* --------------------------------------------------------
       3.1 Desactivar versión anterior
       -------------------------------------------------------- */

    UPDATE versiones_modelo
    SET
        Estado = 'INACTIVA',
        FechaFinVigencia = '2026-07-19'
    WHERE IdVersionModelo = @IdVersionAnterior;


    /* --------------------------------------------------------
       3.2 Crear nueva versión
       -------------------------------------------------------- */

    INSERT INTO versiones_modelo
    (
        IdVersionModelo,
        IdModelo,
        NumeroVersion,
        FechaInicioVigencia,
        FechaFinVigencia,
        Estado,
        FechaCreacion
    )
    VALUES
    (
        @IdVersionNueva,
        @IdModelo,
        '1.1.0',
        '2026-07-20',
        NULL,
        'ACTIVA',
        SYSDATETIMEOFFSET()
    );


    /* ========================================================
       4. Mapeo de factores 1.0.0 → 1.1.0
       ======================================================== */

    DECLARE @FactorMap TABLE
    (
        IdFactorAnterior UNIQUEIDENTIFIER NOT NULL,
        IdFactorNuevo UNIQUEIDENTIFIER NOT NULL,
        Codigo VARCHAR(30) NOT NULL
    );


    INSERT INTO @FactorMap
    (
        IdFactorAnterior,
        IdFactorNuevo,
        Codigo
    )
    SELECT
        IdFactor,
        NEWID(),
        Codigo
    FROM factores_scoring
    WHERE IdVersionModelo = @IdVersionAnterior;


    /* ========================================================
       5. Copiar factores y modificar pesos
       ======================================================== */

    INSERT INTO factores_scoring
    (
        IdFactor,
        IdVersionModelo,
        Codigo,
        Nombre,
        Descripcion,
        Peso,
        Estado
    )
    SELECT
        fm.IdFactorNuevo,
        @IdVersionNueva,
        f.Codigo,
        f.Nombre,
        f.Descripcion,

        CASE f.Codigo

            WHEN 'HISTORIAL_PAGOS'
                THEN 22.50

            WHEN 'RELACION_DEUDA_INGRESO'
                THEN 18.00

            WHEN 'CAPACIDAD_PAGO'
                THEN 18.00

            WHEN 'ESTABILIDAD_INGRESOS'
                THEN 13.50

            WHEN 'ANTIGUEDAD_LABORAL'
                THEN 9.00

            WHEN 'OBLIGACIONES_ACTIVAS'
                THEN 4.50

            WHEN 'MONTO_CAPACIDAD'
                THEN 4.50

            WHEN 'ALERTAS_MORA'
                THEN 0.00

            ELSE f.Peso

        END,

        'ACTIVO'

    FROM factores_scoring f

    INNER JOIN @FactorMap fm
        ON fm.IdFactorAnterior = f.IdFactor

    WHERE f.IdVersionModelo = @IdVersionAnterior;


    /* ========================================================
       6. Copiar reglas de los 8 factores existentes
       ======================================================== */

    INSERT INTO reglas_evaluacion
    (
        IdRegla,
        IdFactor,
        Codigo,
        Descripcion,
        ValorMinimo,
        ValorMaximo,
        Puntaje,
        EsExcluyente,
        ResultadoExcluyente,
        Estado
    )
    SELECT
        NEWID(),
        fm.IdFactorNuevo,
        r.Codigo,
        r.Descripcion,
        r.ValorMinimo,
        r.ValorMaximo,
        r.Puntaje,
        r.EsExcluyente,
        r.ResultadoExcluyente,
        r.Estado

    FROM reglas_evaluacion r

    INNER JOIN @FactorMap fm
        ON fm.IdFactorAnterior = r.IdFactor;


    /* ========================================================
       7. Agregar factor RELACION_CUOTA_INGRESO
       ======================================================== */

    INSERT INTO factores_scoring
    (
        IdFactor,
        IdVersionModelo,
        Codigo,
        Nombre,
        Descripcion,
        Peso,
        Estado
    )
    VALUES
    (
        @IdFactorRci,
        @IdVersionNueva,
        'RELACION_CUOTA_INGRESO',
        'Relación cuota-ingreso',
        'Porcentaje de los ingresos mensuales destinado a la cuota estimada del crédito.',
        10.00,
        'ACTIVO'
    );


    /* ========================================================
       8. Reglas de RELACION_CUOTA_INGRESO
       ======================================================== */

    INSERT INTO reglas_evaluacion
    (
        IdRegla,
        IdFactor,
        Codigo,
        Descripcion,
        ValorMinimo,
        ValorMaximo,
        Puntaje,
        EsExcluyente,
        ResultadoExcluyente,
        Estado
    )
    VALUES

    (
        NEWID(),
        @IdFactorRci,
        'RCI_BAJA',
        'Relación cuota-ingreso baja.',
        0.0000,
        20.0000,
        100,
        0,
        NULL,
        'ACTIVA'
    ),

    (
        NEWID(),
        @IdFactorRci,
        'RCI_MEDIA',
        'Relación cuota-ingreso moderada.',
        20.0001,
        30.0000,
        75,
        0,
        NULL,
        'ACTIVA'
    ),

    (
        NEWID(),
        @IdFactorRci,
        'RCI_ALTA',
        'Relación cuota-ingreso elevada.',
        30.0001,
        40.0000,
        40,
        0,
        NULL,
        'ACTIVA'
    ),

    (
        NEWID(),
        @IdFactorRci,
        'RCI_MUY_ALTA',
        'Relación cuota-ingreso muy elevada.',
        40.0001,
        9999.0000,
        0,
        0,
        NULL,
        'ACTIVA'
    );


    /* ========================================================
       9. Validar suma de pesos
       ======================================================== */

    DECLARE @PesoTotal DECIMAL(7, 2);


    SELECT
        @PesoTotal = SUM(Peso)
    FROM factores_scoring
    WHERE IdVersionModelo = @IdVersionNueva
      AND Estado = 'ACTIVO';


    IF @PesoTotal <> 100.00
    BEGIN

        THROW 50003,
            'Los pesos de la versión 1.1.0 no suman 100%.',
            1;

    END;

END;