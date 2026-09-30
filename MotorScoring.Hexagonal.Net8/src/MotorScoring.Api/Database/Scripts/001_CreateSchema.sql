IF OBJECT_ID('solicitantes', 'U') IS NULL
BEGIN

    CREATE TABLE solicitantes
    (
        IdSolicitante UNIQUEIDENTIFIER NOT NULL,
        TipoDocumento VARCHAR(20) NOT NULL,
        NumeroDocumento VARCHAR(30) NOT NULL,
        NombresRazonSocial VARCHAR(150) NOT NULL,
        IngresosMensuales DECIMAL(18, 2) NOT NULL,
        GastosMensuales DECIMAL(18, 2) NOT NULL,
        ObligacionesFinancieras DECIMAL(18, 2) NOT NULL,
        AntiguedadLaboralNegocio INT NOT NULL,
        NumeroObligacionesActivas INT NOT NULL,
        PuntajeHistorialPagos INT NOT NULL,
        AlertasMora INT NOT NULL,
        Moneda VARCHAR(10) NOT NULL,
        Estado VARCHAR(20) NOT NULL,
        FechaRegistro DATETIMEOFFSET NOT NULL,

        CONSTRAINT PK_solicitantes
            PRIMARY KEY (IdSolicitante),

        CONSTRAINT UQ_solicitante_documento
            UNIQUE (TipoDocumento, NumeroDocumento),

        CONSTRAINT CK_solicitante_ingresos
            CHECK (IngresosMensuales > 0),

        CONSTRAINT CK_historial_pagos
            CHECK (PuntajeHistorialPagos BETWEEN 0 AND 100)
    );


    CREATE TABLE modelos_scoring
    (
        IdModelo UNIQUEIDENTIFIER NOT NULL,
        Codigo VARCHAR(30) NOT NULL,
        Nombre VARCHAR(150) NOT NULL,
        Descripcion VARCHAR(255) NULL,
        Estado VARCHAR(20) NOT NULL,
        FechaCreacion DATETIMEOFFSET NOT NULL
            DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_modelos_scoring
            PRIMARY KEY (IdModelo),

        CONSTRAINT UQ_modelos_scoring_codigo
            UNIQUE (Codigo)
    );


    CREATE TABLE versiones_modelo
    (
        IdVersionModelo UNIQUEIDENTIFIER NOT NULL,
        IdModelo UNIQUEIDENTIFIER NOT NULL,
        NumeroVersion VARCHAR(20) NOT NULL,
        FechaInicioVigencia DATE NOT NULL,
        FechaFinVigencia DATE NULL,
        Estado VARCHAR(20) NOT NULL,
        FechaCreacion DATETIMEOFFSET NOT NULL
            DEFAULT SYSDATETIMEOFFSET(),

        CONSTRAINT PK_versiones_modelo
            PRIMARY KEY (IdVersionModelo),

        CONSTRAINT FK_version_modelo
            FOREIGN KEY (IdModelo)
            REFERENCES modelos_scoring (IdModelo),

        CONSTRAINT UQ_modelo_version
            UNIQUE (IdModelo, NumeroVersion)
    );


    CREATE TABLE factores_scoring
    (
        IdFactor UNIQUEIDENTIFIER NOT NULL,
        IdVersionModelo UNIQUEIDENTIFIER NOT NULL,
        Codigo VARCHAR(30) NOT NULL,
        Nombre VARCHAR(150) NOT NULL,
        Descripcion VARCHAR(255) NULL,
        Peso DECIMAL(5, 2) NOT NULL,
        Estado VARCHAR(20) NOT NULL,

        CONSTRAINT PK_factores_scoring
            PRIMARY KEY (IdFactor),

        CONSTRAINT FK_factor_version
            FOREIGN KEY (IdVersionModelo)
            REFERENCES versiones_modelo (IdVersionModelo),

        CONSTRAINT UQ_factor_version_codigo
            UNIQUE (IdVersionModelo, Codigo),

        CONSTRAINT CK_factor_peso
            CHECK (Peso BETWEEN 0 AND 100)
    );


    CREATE TABLE reglas_evaluacion
    (
        IdRegla UNIQUEIDENTIFIER NOT NULL,
        IdFactor UNIQUEIDENTIFIER NOT NULL,
        Codigo VARCHAR(30) NOT NULL,
        Descripcion VARCHAR(255) NOT NULL,
        ValorMinimo DECIMAL(18, 4) NOT NULL,
        ValorMaximo DECIMAL(18, 4) NOT NULL,
        Puntaje INT NOT NULL,
        EsExcluyente BIT NOT NULL
            DEFAULT 0,
        ResultadoExcluyente VARCHAR(30) NULL,
        Estado VARCHAR(20) NOT NULL,

        CONSTRAINT PK_reglas_evaluacion
            PRIMARY KEY (IdRegla),

        CONSTRAINT FK_regla_factor
            FOREIGN KEY (IdFactor)
            REFERENCES factores_scoring (IdFactor),

        CONSTRAINT UQ_regla_factor_codigo
            UNIQUE (IdFactor, Codigo),

        CONSTRAINT CK_regla_rango
            CHECK (ValorMaximo >= ValorMinimo),

        CONSTRAINT CK_regla_puntaje
            CHECK (Puntaje BETWEEN 0 AND 100)
    );


    CREATE TABLE productos_crediticios
    (
        IdProducto UNIQUEIDENTIFIER NOT NULL,
        Codigo VARCHAR(30) NOT NULL,
        Nombre VARCHAR(150) NOT NULL,
        MontoMinimo DECIMAL(18, 2) NOT NULL,
        MontoMaximo DECIMAL(18, 2) NOT NULL,
        PlazoMinimo INT NOT NULL,
        PlazoMaximo INT NOT NULL,
        Moneda VARCHAR(10) NOT NULL,
        Estado VARCHAR(20) NOT NULL,
        IdModeloScoring UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT PK_productos_crediticios
            PRIMARY KEY (IdProducto),

        CONSTRAINT UQ_productos_crediticios_codigo
            UNIQUE (Codigo),

        CONSTRAINT FK_producto_modelo
            FOREIGN KEY (IdModeloScoring)
            REFERENCES modelos_scoring (IdModelo)
    );


    CREATE TABLE solicitudes_credito
    (
        IdSolicitud UNIQUEIDENTIFIER NOT NULL,
        IdSolicitante UNIQUEIDENTIFIER NOT NULL,
        IdProducto UNIQUEIDENTIFIER NOT NULL,
        MontoSolicitado DECIMAL(18, 2) NOT NULL,
        PlazoSolicitado INT NOT NULL,
        Moneda VARCHAR(10) NOT NULL,
        FinalidadCredito VARCHAR(150) NOT NULL,
        CanalOrigen VARCHAR(50) NOT NULL,
        FechaRegistro DATETIMEOFFSET NOT NULL,
        IdentificadorExterno VARCHAR(100) NOT NULL,
        Estado VARCHAR(20) NOT NULL,

        CONSTRAINT PK_solicitudes_credito
            PRIMARY KEY (IdSolicitud),

        CONSTRAINT FK_solicitud_solicitante
            FOREIGN KEY (IdSolicitante)
            REFERENCES solicitantes (IdSolicitante),

        CONSTRAINT FK_solicitud_producto
            FOREIGN KEY (IdProducto)
            REFERENCES productos_crediticios (IdProducto),

        CONSTRAINT UQ_solicitud_identificador_externo
            UNIQUE (IdentificadorExterno)
    );


    CREATE TABLE evaluaciones_crediticias
    (
        IdEvaluacion UNIQUEIDENTIFIER NOT NULL,
        IdSolicitud UNIQUEIDENTIFIER NOT NULL,
        IdVersionModelo UNIQUEIDENTIFIER NOT NULL,
        FechaEvaluacion DATETIMEOFFSET NOT NULL,
        PuntajeTotal INT NOT NULL,
        Resultado VARCHAR(30) NOT NULL,
        Estado VARCHAR(30) NOT NULL,

        CONSTRAINT PK_evaluaciones_crediticias
            PRIMARY KEY (IdEvaluacion),

        CONSTRAINT FK_evaluacion_solicitud
            FOREIGN KEY (IdSolicitud)
            REFERENCES solicitudes_credito (IdSolicitud),

        CONSTRAINT FK_evaluacion_version
            FOREIGN KEY (IdVersionModelo)
            REFERENCES versiones_modelo (IdVersionModelo),

        CONSTRAINT UQ_evaluacion_solicitud_version
            UNIQUE (IdSolicitud, IdVersionModelo),

        CONSTRAINT CK_evaluacion_puntaje
            CHECK (PuntajeTotal BETWEEN 0 AND 1000)
    );


    CREATE TABLE resultados_factor
    (
        IdResultadoFactor UNIQUEIDENTIFIER NOT NULL,
        IdEvaluacion UNIQUEIDENTIFIER NOT NULL,
        IdFactor UNIQUEIDENTIFIER NOT NULL,
        CodigoFactor VARCHAR(30) NOT NULL,
        ValorEvaluado DECIMAL(18, 4) NOT NULL,
        PesoAplicado DECIMAL(5, 2) NOT NULL,
        PuntajeBase INT NOT NULL,
        PuntajeObtenido INT NOT NULL,
        ReglaAplicada VARCHAR(30) NOT NULL,
        Observacion VARCHAR(255) NULL,
        ReglaExcluyente BIT NOT NULL,
        ResultadoExcluyente VARCHAR(30) NULL,

        CONSTRAINT PK_resultados_factor
            PRIMARY KEY (IdResultadoFactor),

        CONSTRAINT FK_resultado_factor_evaluacion
            FOREIGN KEY (IdEvaluacion)
            REFERENCES evaluaciones_crediticias (IdEvaluacion),

        CONSTRAINT FK_resultado_factor_factor
            FOREIGN KEY (IdFactor)
            REFERENCES factores_scoring (IdFactor),

        CONSTRAINT UQ_resultado_factor_evaluacion_factor
            UNIQUE (IdEvaluacion, IdFactor)
    );


    CREATE TABLE resultados_scoring
    (
        IdResultadoScoring UNIQUEIDENTIFIER NOT NULL,
        IdEvaluacion UNIQUEIDENTIFIER NOT NULL,
        PuntajeTotal INT NOT NULL,
        Resultado VARCHAR(30) NOT NULL,
        FechaResultado DATETIMEOFFSET NOT NULL,

        CONSTRAINT PK_resultados_scoring
            PRIMARY KEY (IdResultadoScoring),

        CONSTRAINT UQ_resultados_scoring_evaluacion
            UNIQUE (IdEvaluacion),

        CONSTRAINT FK_resultado_scoring_evaluacion
            FOREIGN KEY (IdEvaluacion)
            REFERENCES evaluaciones_crediticias (IdEvaluacion),

        CONSTRAINT CK_resultado_scoring_puntaje
            CHECK (PuntajeTotal BETWEEN 0 AND 1000)
    );

END;