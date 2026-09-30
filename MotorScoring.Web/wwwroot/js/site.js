const MotorScoringWeb = (() => {
    const initCrearSolicitud = () => {
        const form = document.getElementById("frmSolicitud");
        const clearButton = document.getElementById("btnLimpiar");
        const submitButton = document.getElementById("btnRegistrar");

        if (!form) return;

        clearButton?.addEventListener("click", () => {
            form.reset();
        });

        form.addEventListener("submit", () => {
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.textContent = "Registrando...";
            }
        });
    };

    const init = () => {
        initCrearSolicitud();
    };

    return { init };
})();

document.addEventListener("DOMContentLoaded", MotorScoringWeb.init);
