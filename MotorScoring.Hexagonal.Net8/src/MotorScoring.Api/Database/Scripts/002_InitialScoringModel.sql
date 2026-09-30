DECLARE
    @modelo UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111',
    @version UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111112';

IF NOT EXISTS (
    SELECT 1
    FROM modelos_scoring
    WHERE IdModelo = @modelo
)
BEGIN

    INSERT INTO modelos_scoring
    (
        IdModelo,
        Codigo,
        Nombre,
        Descripcion,
        Estado,
        FechaCreacion
    )
    VALUES
    (
        @modelo,
        'MODELO_PERSONAL',
        'Modelo de préstamo personal',
        'Modelo inicial RF04-RF06',
        'ACTIVO',
        SYSDATETIMEOFFSET()
    );


    INSERT INTO versiones_modelo
    (
        IdVersionModelo,
        IdModelo,
        NumeroVersion,
        FechaInicioVigencia,
        FechaFinVigencia,
        Estado
    )
    VALUES
    (
        @version,
        @modelo,
        '1.0.0',
        '2025-01-01',
        NULL,
        'ACTIVA'
    );


    DECLARE @factores TABLE
    (
        IdFactor UNIQUEIDENTIFIER,
        Codigo VARCHAR(30),
        Nombre VARCHAR(150),
        Descripcion VARCHAR(255),
        Peso DECIMAL(5, 2)
    );

    INSERT INTO @factores
    (
        IdFactor,
        Codigo,
        Nombre,
        Descripcion,
        Peso
    )
    VALUES
    (
        '20000000-0000-0000-0000-000000000001',
        'HISTORIAL_PAGOS',
        'Historial de pagos',
        'Comportamiento histórico',
        25.00
    ),
    (
        '20000000-0000-0000-0000-000000000002',
        'RELACION_DEUDA_INGRESO',
        'Relación deuda-ingreso',
        'Obligaciones respecto del ingreso',
        20.00
    ),
    (
        '20000000-0000-0000-0000-000000000003',
        'CAPACIDAD_PAGO',
        'Capacidad de pago',
        'Ingreso disponible',
        20.00
    ),
    (
        '20000000-0000-0000-0000-000000000004',
        'ESTABILIDAD_INGRESOS',
        'Estabilidad de ingresos',
        'Continuidad de ingresos',
        15.00
    ),
    (
        '20000000-0000-0000-0000-000000000005',
        'ANTIGUEDAD_LABORAL',
        'Antigüedad laboral',
        'Meses de permanencia',
        10.00
    ),
    (
        '20000000-0000-0000-0000-000000000006',
        'OBLIGACIONES_ACTIVAS',
        'Obligaciones activas',
        'Cantidad de obligaciones',
        5.00
    ),
    (
        '20000000-0000-0000-0000-000000000007',
        'MONTO_CAPACIDAD',
        'Monto frente a capacidad',
        'Monto solicitado respecto a capacidad',
        5.00
    ),
    (
        '20000000-0000-0000-0000-000000000008',
        'ALERTAS_MORA',
        'Alertas de mora',
        'Regla excluyente',
        0.00
    );


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
        IdFactor,
        @version,
        Codigo,
        Nombre,
        Descripcion,
        Peso,
        'ACTIVO'
    FROM @factores;


    DECLARE @reglas TABLE
    (
        IdFactor UNIQUEIDENTIFIER,
        Codigo VARCHAR(30),
        Descripcion VARCHAR(255),
        ValorMinimo DECIMAL(18, 4),
        ValorMaximo DECIMAL(18, 4),
        Puntaje INT,
        EsExcluyente BIT,
        ResultadoExcluyente VARCHAR(30)
    );


    INSERT INTO @reglas
    (
        IdFactor,
        Codigo,
        Descripcion,
        ValorMinimo,
        ValorMaximo,
        Puntaje,
        EsExcluyente,
        ResultadoExcluyente
    )
    VALUES

    -- HISTORIAL_PAGOS
    (
        '20000000-0000-0000-0000-000000000001',
        'HP_BAJO',
        'Historial deficiente',
        0,
        39.9999,
        20,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000001',
        'HP_REGULAR',
        'Historial regular',
        40,
        59.9999,
        50,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000001',
        'HP_BUENO',
        'Historial bueno',
        60,
        79.9999,
        75,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000001',
        'HP_EXCELENTE',
        'Historial excelente',
        80,
        100,
        100,
        0,
        NULL
    ),

    -- RELACION_DEUDA_INGRESO
    (
        '20000000-0000-0000-0000-000000000002',
        'RDI_BAJA',
        'Endeudamiento bajo',
        0,
        20,
        100,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000002',
        'RDI_MEDIA',
        'Endeudamiento moderado',
        20.0001,
        35,
        80,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000002',
        'RDI_ALTA',
        'Endeudamiento elevado',
        35.0001,
        50,
        50,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000002',
        'RDI_MUY_ALTA',
        'Endeudamiento muy elevado',
        50.0001,
        9999,
        20,
        0,
        NULL
    ),

    -- CAPACIDAD_PAGO
    (
        '20000000-0000-0000-0000-000000000003',
        'CP_CRITICA',
        'Capacidad crítica',
        0,
        9.9999,
        20,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000003',
        'CP_BAJA',
        'Capacidad baja',
        10,
        24.9999,
        60,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000003',
        'CP_MEDIA',
        'Capacidad media',
        25,
        39.9999,
        80,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000003',
        'CP_ALTA',
        'Capacidad alta',
        40,
        100,
        100,
        0,
        NULL
    ),

    -- ESTABILIDAD_INGRESOS
    (
        '20000000-0000-0000-0000-000000000004',
        'EI_BAJA',
        'Ingresos poco estables',
        0,
        5,
        20,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000004',
        'EI_MEDIA',
        'Estabilidad inicial',
        6,
        11,
        40,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000004',
        'EI_BUENA',
        'Ingresos estables',
        12,
        23,
        70,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000004',
        'EI_ALTA',
        'Ingresos muy estables',
        24,
        9999,
        100,
        0,
        NULL
    ),

    -- ANTIGUEDAD_LABORAL
    (
        '20000000-0000-0000-0000-000000000005',
        'AL_BAJA',
        'Antigüedad menor a un año',
        0,
        11,
        25,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000005',
        'AL_MEDIA',
        'Antigüedad entre uno y tres años',
        12,
        35,
        60,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000005',
        'AL_ALTA',
        'Antigüedad mayor a tres años',
        36,
        9999,
        100,
        0,
        NULL
    ),

    -- OBLIGACIONES_ACTIVAS
    (
        '20000000-0000-0000-0000-000000000006',
        'OA_BAJA',
        'Pocas obligaciones',
        0,
        1,
        100,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000006',
        'OA_MEDIA',
        'Obligaciones moderadas',
        2,
        3,
        75,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000006',
        'OA_ALTA',
        'Varias obligaciones',
        4,
        5,
        50,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000006',
        'OA_MUY_ALTA',
        'Demasiadas obligaciones',
        6,
        9999,
        20,
        0,
        NULL
    ),

    -- MONTO_CAPACIDAD
    (
        '20000000-0000-0000-0000-000000000007',
        'MC_BAJA',
        'Monto conservador',
        0,
        30,
        100,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000007',
        'MC_MEDIA',
        'Monto moderado',
        30.0001,
        60,
        75,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000007',
        'MC_ALTA',
        'Monto elevado',
        60.0001,
        100,
        50,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000007',
        'MC_MUY_ALTA',
        'Monto superior a capacidad',
        100.0001,
        9999,
        10,
        0,
        NULL
    ),

    -- ALERTAS_MORA
    (
        '20000000-0000-0000-0000-000000000008',
        'AM_SIN_ALERTAS',
        'Sin alertas',
        0,
        0,
        100,
        0,
        NULL
    ),
    (
        '20000000-0000-0000-0000-000000000008',
        'AM_CON_ALERTAS',
        'Mora vigente',
        1,
        9999,
        0,
        1,
        'RECHAZADA'
    );


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
        IdFactor,
        Codigo,
        Descripcion,
        ValorMinimo,
        ValorMaximo,
        Puntaje,
        EsExcluyente,
        ResultadoExcluyente,
        'ACTIVA'
    FROM @reglas;


    INSERT INTO productos_crediticios
    (
        IdProducto,
        Codigo,
        Nombre,
        MontoMinimo,
        MontoMaximo,
        PlazoMinimo,
        PlazoMaximo,
        Moneda,
        Estado,
        IdModeloScoring
    )
    VALUES
    (
        '33333333-3333-3333-3333-333333333333',
        'PRESTAMO_PERSONAL',
        'Préstamo personal',
        1000.00,
        50000.00,
        6,
        48,
        'PEN',
        'ACTIVO',
        @modelo
    );

END;