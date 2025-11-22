// SPA State Management for Reservar Page
const reservaState = {
    servicioID: null,
    servicioNombre: null,
    servicioPrecio: null,
    barberoID: null,
    barberoNombre: null,
    fecha: null,
    horarioID: null,
    horarioDisplay: null,
    horaInicio: null,
    horaFin: null
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeServicioSelection();
    setupDatePicker();
    setupConfirmFormHandler();
});

// ========== STEP 1: Servicio Selection ==========
function initializeServicioSelection() {
    const servicioCards = document.querySelectorAll('.servicio-card');

    servicioCards.forEach(card => {
        card.addEventListener('click', function () {
            // Remove selection from all cards
            servicioCards.forEach(c => {
                c.classList.remove('border-primary', 'scale-105');
                c.querySelector('.selected-badge').classList.add('hidden');
            });

            // Mark this card as selected
            this.classList.add('border-primary', 'scale-105');
            this.querySelector('.selected-badge').classList.remove('hidden');

            // Update state
            reservaState.servicioID = this.dataset.servicioId;
            reservaState.servicioNombre = this.dataset.servicioNombre;
            reservaState.servicioPrecio = this.dataset.servicioPrecio;

            console.log('Servicio seleccionado', { servicioID: reservaState.servicioID, servicioNombre: reservaState.servicioNombre, duracion: this.dataset.servicioDuracion });

            // update hidden inputs in case user proceeds immediately
            updateFormInputs();

            // Update summary
            updateSummary();

            // Update progress
            updateProgress(1);

            // Load barberos and show next section
            loadBarberos();

            // Smooth scroll to barberos section
            setTimeout(() => {
                const barberoSection = document.getElementById('section-barbero');
                barberoSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 300);
        });
    });
}

// ========== STEP 2: Barbero Selection ==========
async function loadBarberos() {
    const barberosGrid = document.getElementById('barberos-grid');
    const barberoSection = document.getElementById('section-barbero');

    // Show section
    barberoSection.classList.remove('hidden');

    // Show loading
    barberosGrid.innerHTML = '<div class="col-span-2 text-center py-8"><span class="loading loading-spinner loading-lg text-primary"></span></div>';

    try {
        const response = await fetch(`/Api/GetBarberos`);
        const barberos = await response.json();

        if (barberos.length === 0) {
            barberosGrid.innerHTML = '<div class="col-span-2 text-center py-8 opacity-70">No hay barberos disponibles</div>';
            return;
        }

        // Render barberos
        barberosGrid.innerHTML = barberos.map(barbero => {
            const disponibilidad = barbero.disponibilidad || 'Disponible';
            const esDisponible = disponibilidad === 'Disponible';

            // Get badge configuration based on availability
            const badgeConfig = {
                'Disponible': { class: 'badge-success', icon: '🟢', text: 'Disponible' },
                'No Disponible': { class: 'badge-error', icon: '🔴', text: 'No Disponible' },
                'Ocupado': { class: 'badge-warning', icon: '🟡', text: 'Ocupado' }
            };
            const badge = badgeConfig[disponibilidad] || badgeConfig['Disponible'];

            return `
            <div class="premium-card group ${esDisponible ? 'cursor-pointer' : 'opacity-60 cursor-not-allowed'} barbero-card" 
                 data-barbero-id="${barbero.barberoID}" 
                 data-barbero-nombre="${barbero.nombre}"
                 data-barbero-disponibilidad="${disponibilidad}">
                <div class="card-body">
                    <div class="flex items-center gap-4 mb-3">
                        <div class="avatar placeholder">
                            <div class="bg-gradient-to-br from-primary to-accent text-black rounded-full w-16">
                                <span class="text-2xl font-bold">${barbero.nombre.charAt(0)}</span>
                            </div>
                        </div>
                        <div class="flex-1">
                            <h3 class="card-title text-xl">${barbero.nombre}</h3>
                            <p class="text-sm opacity-60">${barbero.especialidades || 'Barbero profesional'}</p>
                            <div class="mt-2">
                                <div class="badge ${badge.class} gap-2">
                                    <span>${badge.icon}</span>
                                    ${badge.text}
                                </div>
                            </div>
                        </div>
                        <div class="badge badge-primary badge-lg shadow-lg hidden selected-badge">Seleccionado</div>
                    </div>
                </div>
            </div>
        `;
        }).join('');

        // Add click handlers
        initializeBarberoSelection();

    } catch (error) {
        console.error('Error loading barberos:', error);
        barberosGrid.innerHTML = '<div class="col-span-2 text-center py-8 text-error">Error al cargar barberos</div>';
    }
}

function initializeBarberoSelection() {
    const barberoCards = document.querySelectorAll('.barbero-card');

    barberoCards.forEach(card => {
        card.addEventListener('click', function () {
            // Check availability first
            const disponibilidad = this.dataset.barberoDisponibilidad || 'Disponible';

            if (disponibilidad !== 'Disponible') {
                // Show error message for unavailable barbers
                const messages = {
                    'No Disponible': 'Este barbero no está disponible en este momento. Por favor selecciona otro barbero.',
                    'Ocupado': 'Este barbero está ocupado actualmente. Por favor selecciona otro barbero.'
                };
                alert(messages[disponibilidad] || 'Este barbero no está disponible.');
                return; // Prevent selection
            }

            // Remove selection from all cards
            barberoCards.forEach(c => {
                c.classList.remove('border-primary', 'scale-105');
                c.querySelector('.selected-badge').classList.add('hidden');
            });

            // Mark this card as selected
            this.classList.add('border-primary', 'scale-105');
            this.querySelector('.selected-badge').classList.remove('hidden');

            // Update state
            reservaState.barberoID = this.dataset.barberoId;
            reservaState.barberoNombre = this.dataset.barberoNombre;

            console.log('Barbero seleccionado', { barberoID: reservaState.barberoID, barberoNombre: reservaState.barberoNombre });

            // update hidden inputs so backend receives current state
            updateFormInputs();

            // Update summary
            updateSummary();

            // Update progress
            updateProgress(2);

            // Show horario section
            const horarioSection = document.getElementById('section-horario');
            horarioSection.classList.remove('hidden');

            // Set min date to today
            const fechaInput = document.getElementById('fechaInput');
            const today = new Date().toISOString().split('T')[0];
            fechaInput.min = today;

            // Smooth scroll to horario section
            setTimeout(() => {
                horarioSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 300);
        });
    });
}

// ========== STEP 3: Fecha & Horario Selection ==========
function setupDatePicker() {
    const fechaInput = document.getElementById('fechaInput');

    fechaInput.addEventListener('change', function () {
        const fecha = this.value;
        if (fecha && reservaState.barberoID) {
            reservaState.fecha = fecha;
            loadHorarios(reservaState.barberoID, fecha);
            console.log('Fecha seleccionada', fecha);

            // update hidden input immediately
            updateFormInputs();
        }
    });
}

async function loadHorarios(barberoId, fecha) {
    const horariosGrid = document.getElementById('horarios-grid');
    const horariosLoading = document.getElementById('horarios-loading');
    const horariosEmpty = document.getElementById('horarios-empty');

    // Show loading
    horariosLoading.classList.remove('hidden');
    horariosEmpty.classList.add('hidden');
    horariosGrid.innerHTML = '';

    try {
        // determine duration and include servicioId so server can compute duration and step reliably
        let duracion = 60;
        let servicioIdParam = '';
        if (reservaState.servicioID) {
            const svcCard = document.querySelector(`.servicio-card[data-servicio-id="${reservaState.servicioID}"]`);
            if (svcCard && svcCard.dataset && svcCard.dataset.servicioDuracion) {
                duracion = parseInt(svcCard.dataset.servicioDuracion, 10) || 60;
            }
            servicioIdParam = `&servicioId=${encodeURIComponent(reservaState.servicioID)}`;
        }
        const response = await fetch(`/Api/GetHorarios?barberoId=${barberoId}&fecha=${fecha}&duracion=${duracion}${servicioIdParam}`);
        const horarios = await response.json();

        // Hide loading
        horariosLoading.classList.add('hidden');

        if (horarios.length === 0) {
            horariosEmpty.classList.remove('hidden');
            return;
        }

        // Render horarios
        horariosGrid.innerHTML = horarios.map(horario => `
            <div class="premium-card cursor-pointer horario-card" data-horario-id="${horario.horarioID}" data-horario-display="${horario.displayText}" data-horario-hora="${horario.horaInicio}" data-hora-inicio="${horario.horaInicio}" data-hora-fin="${horario.horaFin}">
                <div class="card-body p-4 text-center">
                    <div class="text-sm opacity-60 mb-1">${horario.fechaDisplay}</div>
                    <div class="text-2xl font-black bg-gradient-to-r from-primary to-accent bg-clip-text text-transparent">
                        ${horario.horaInicio}
                    </div>
                    <div class="text-xs opacity-50 mt-1">a ${horario.horaFin}</div>
                    <div class="badge badge-primary badge-sm shadow-lg hidden selected-badge mt-2">Seleccionado</div>
                </div>
            </div>
        `).join('');

        // Add click handlers
        initializeHorarioSelection();

    } catch (error) {
        console.error('Error loading horarios:', error);
        horariosLoading.classList.add('hidden');
        horariosGrid.innerHTML = '<div class="col-span-3 text-center py-8 text-error">Error al cargar horarios</div>';
    }
}

function initializeHorarioSelection() {
    const horarioCards = document.querySelectorAll('.horario-card');

    horarioCards.forEach(card => {
        card.addEventListener('click', function () {
            // Remove selection from all cards
            horarioCards.forEach(c => {
                c.classList.remove('border-primary', 'scale-105');
                c.querySelector('.selected-badge').classList.add('hidden');
            });

            // Mark this card as selected
            this.classList.add('border-primary', 'scale-105');
            this.querySelector('.selected-badge').classList.remove('hidden');

            // Update state
            reservaState.horarioID = this.dataset.horarioId;
            reservaState.horarioDisplay = this.dataset.horarioDisplay;
            // Read hora values from dataset (data-hora-inicio / data-hora-fin)
            reservaState.horaInicio = this.dataset.horaInicio;
            reservaState.horaFin = this.dataset.horaFin;

            console.log('Slot seleccionado', { horarioID: reservaState.horarioID, horaInicio: reservaState.horaInicio, horaFin: reservaState.horaFin });

            // Update summary
            updateSummary();

            // Update progress
            updateProgress(3);

            // Enable confirm button
            const confirmButton = document.getElementById('confirmButton');
            confirmButton.disabled = false;
            confirmButton.classList.remove('btn-disabled');

            // Update form hidden inputs
            updateFormInputs();

            // Smooth scroll to summary
            setTimeout(() => {
                const summary = document.querySelector('.card.bg-base-200');
                summary.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }, 300);
        });
    });
}

// ========== Update Functions ==========
function updateSummary() {
    // Update servicio
    if (reservaState.servicioNombre) {
        document.getElementById('summary-servicio').textContent = reservaState.servicioNombre;
        document.getElementById('summary-servicio').classList.remove('opacity-40');
        document.getElementById('summary-precio').textContent = `$${reservaState.servicioPrecio}`;
        document.getElementById('summary-precio').classList.remove('hidden');
    }

    // Update barbero
    if (reservaState.barberoNombre) {
        document.getElementById('summary-barbero').textContent = reservaState.barberoNombre;
        document.getElementById('summary-barbero').classList.remove('opacity-40');
    }

    // Update fecha y hora
    if (reservaState.fecha && reservaState.horarioDisplay) {
        const fechaObj = new Date(reservaState.fecha + 'T00:00:00');
        const fechaFormatted = fechaObj.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit', year: 'numeric' });

        document.getElementById('summary-fecha').textContent = fechaFormatted;
        document.getElementById('summary-fecha').classList.remove('opacity-40');
        document.getElementById('summary-hora').textContent = reservaState.horarioDisplay.split(' - ')[1];
        document.getElementById('summary-hora').classList.remove('hidden');
    }
}

function updateProgress(step) {
    const steps = ['step1', 'step2', 'step3', 'step4'];

    steps.forEach((stepId, index) => {
        const stepElement = document.getElementById(stepId);
        if (index < step) {
            stepElement.classList.add('step-primary');
            stepElement.setAttribute('data-content', '✓');
        } else if (index === step) {
            stepElement.classList.add('step-primary');
            stepElement.setAttribute('data-content', index + 1);
        }
    });
}

function updateFormInputs() {
    document.getElementById('form-servicio').value = reservaState.servicioID || '';
    document.getElementById('form-barbero').value = reservaState.barberoID || '';
    document.getElementById('form-fecha').value = reservaState.fecha || '';
    document.getElementById('form-horario').value = reservaState.horarioID || '';
    document.getElementById('form-hora-inicio').value = reservaState.horaInicio || '';
    document.getElementById('form-hora-fin').value = reservaState.horaFin || '';
}

function setupConfirmFormHandler() {
    const form = document.getElementById('confirmForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        // Ensure latest values are set
        updateFormInputs();

        let svc = document.getElementById('form-servicio').value;
        let bar = document.getElementById('form-barbero').value;
        let fecha = document.getElementById('form-fecha').value;
        let hor = document.getElementById('form-horario').value;
        let hin = document.getElementById('form-hora-inicio').value;
        let hfin = document.getElementById('form-hora-fin').value;

        console.log('Submitting reservation form with values:', { svc, bar, fecha, hor, hin, hfin });

        // If horario id exists but hora values are empty, try to pull them from DOM horario-card
        if ((hin === '' || hfin === '') && hor) {
            const horarioCard = document.querySelector(`.horario-card[data-horario-id="${hor}"]`);
            if (horarioCard) {
                // dataset uses camelCase for hyphenated data attributes
                const domHin = horarioCard.dataset.horaInicio || horarioCard.dataset.hora || '';
                const domHfin = horarioCard.dataset.horaFin || '';
                if (domHin) {
                    document.getElementById('form-hora-inicio').value = domHin;
                    reservaState.horaInicio = domHin;
                    hin = domHin;
                }
                if (domHfin) {
                    document.getElementById('form-hora-fin').value = domHfin;
                    reservaState.horaFin = domHfin;
                    hfin = domHfin;
                }

                console.log('Filled missing hora values from DOM horario card:', { domHin, domHfin });
            }
        }

        if (!svc || !bar || !fecha || !hor || !hin || !hfin) {
            e.preventDefault();
            alert('Debes completar todos los pasos antes de confirmar la reserva.');
            return false;
        }

        // allow submit
        return true;
    });
}
