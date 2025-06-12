// Токен и состояние аутентификации (хранится в замыкании)
const authState = (() => {
    let accessToken = null;
    let tokenExpiration = null;

    // Функция для парсинга JWT токена
    const parseJwt = (token) => {
        try {
            const tokenParts = token.split('.');
            if (tokenParts.length !== 3) throw new Error('Invalid JWT format');

            const payload = JSON.parse(atob(tokenParts[1]));
            if (!payload.exp) throw new Error('JWT missing expiration');

            return {
                payload,
                expiresAt: payload.exp * 1000 // Конвертируем в миллисекунды
            };
        } catch (e) {
            console.error('JWT parsing error:', e);
            return null;
        }
    };

    return {
        getToken: () => accessToken,
        setToken: (token) => {
            const parsed = parseJwt(token);
            if (!parsed) {
                console.error('Invalid JWT token');
                return false;
            }

            accessToken = token;
            tokenExpiration = new Date(parsed.expiresAt);
            return true;
        },
        clearToken: () => {
            accessToken = null;
            tokenExpiration = null;
        },
        isTokenValid: () => {
            if (!accessToken || !tokenExpiration) return false;
            return new Date() < tokenExpiration;
        }
    };
})();
let isAmenitiesInited = false;
// Улучшенная генерация fingerprint
function generateFingerprint() {
    // Базовые стабильные характеристики браузера
    const fingerprintData = {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language,
        screenResolution: `${window.screen.width}x${window.screen.height}`,
        colorDepth: screen.colorDepth,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        hardwareConcurrency: navigator.hardwareConcurrency || 'unknown',
        deviceMemory: navigator.deviceMemory || 'unknown'
    };

    // Преобразуем данные в строку и создаём простой хэш
    const dataString = JSON.stringify(fingerprintData);
    let hash = 0;

    for (let i = 0; i < dataString.length; i++) {
        const char = dataString.charCodeAt(i);
        hash = (hash << 5) - hash + char;
        hash |= 0; // Преобразуем в 32-битное целое
    }

    // Сохраняем в localStorage для последующих посещений
    return `fp_${Math.abs(hash).toString(36)}`;
}

// Обновление токена
async function refreshToken() {
    try {
        const fingerprint = generateFingerprint();
        const response = await fetch(`/auth/refresh?fingerprint=${fingerprint}`, {
            method: 'POST',
            credentials: 'include',
        });

        if (response.ok) {
            const data = await response.json();
            if (authState.setToken(data.accessToken)) {
                updateAuthUI(true);
                return true;
            }
        } else {
            // Не удалось обновить токен
            authState.clearToken();
            updateAuthUI(false);
            return false;
        }
    } catch (error) {
        console.error('Ошибка при обновлении токена:', error);
        authState.clearToken();
        updateAuthUI(false);
        return false;
    }
}

// Функция для авторизованных запросов
async function authFetch(url, options = {}) {
    if (!authState.isTokenValid()) {
        const refreshed = await refreshToken();
        if (!refreshed) {
            throw new Error('Сессия истекла. Пожалуйста, войдите снова.');
        }
    }

    const defaultHeaders = {
        'Authorization': `Bearer ${authState.getToken()}`
    };

    const defaultOptions = {
        credentials: 'include',
        headers: defaultHeaders
    };

    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultHeaders,
            ...(options.headers || {})
        }
    };

    return fetch(url, mergedOptions);
}

// Обновление UI в зависимости от состояния аутентификации
function updateAuthUI(isAuthenticated) {
    const profileNavItem = document.getElementById('profile-nav-item');
    const authButtons = document.getElementById('auth-buttons');

    if (isAuthenticated) {
        profileNavItem.style.display = 'block';
        authButtons.style.display = 'none';
    } else {
        profileNavItem.style.display = 'none';
        authButtons.style.display = 'flex';
    }
}

// Обработчики модальных окон
document.getElementById('login-btn').addEventListener('click', () => {
    document.getElementById('login-modal').style.display = 'flex';
    document.getElementById('login-error').style.display = 'none';
});

document.getElementById('register-btn').addEventListener('click', () => {
    document.getElementById('register-modal').style.display = 'flex';
    document.getElementById('register-error').style.display = 'none';
});

document.getElementById('close-login').addEventListener('click', () => {
    document.getElementById('login-modal').style.display = 'none';
});

document.getElementById('close-register').addEventListener('click', () => {
    document.getElementById('register-modal').style.display = 'none';
});

document.getElementById('switch-to-register').addEventListener('click', (e) => {
    e.preventDefault();
    document.getElementById('login-modal').style.display = 'none';
    document.getElementById('register-modal').style.display = 'flex';
});

document.getElementById('switch-to-login').addEventListener('click', (e) => {
    e.preventDefault();
    document.getElementById('register-modal').style.display = 'none';
    document.getElementById('login-modal').style.display = 'flex';
});

// Закрытие модальных окон при клике вне их
window.addEventListener('click', (e) => {
    if (e.target.classList.contains('modal')) {
        e.target.style.display = 'none';
    }
});

// Обработка формы входа
document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;
    let isValid = true;

    // Валидация email
    if (!validateEmail(email)) {
        showError('login-email-error', 'Введите корректный email');
        isValid = false;
    } else {
        hideError('login-email-error');
    }

    // Валидация пароля
    if (password.length < 8) {
        showError('login-password-error', 'Пароль должен содержать минимум 8 символов');
        isValid = false;
    } else {
        hideError('login-password-error');
    }

    if (!isValid) return;

    try {
        const fingerprint = generateFingerprint();
        const response = await fetch('/auth/login', {
            method: 'POST',
            credentials: 'include', // Для работы с куками
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email,
                password,
                fingerprint
            })
        });

        if (response.ok) {
            const data = await response.json();
            if (authState.setToken(data.accessToken)) {
                updateAuthUI(true);
                document.getElementById('login-modal').style.display = 'none';
            }
        } else {
            document.getElementById('login-error').style.display = 'block';
        }
    } catch (error) {
        console.error('Ошибка при входе:', error);
        document.getElementById('login-error').style.display = 'block';
    }
});

// Обработка формы регистрации
document.getElementById('register-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const name = document.getElementById('register-name').value;
    const email = document.getElementById('register-email').value;
    const password = document.getElementById('register-password').value;
    let isValid = true;


    // Валидация email
    if (!validateEmail(email)) {
        showError('register-email-error', 'Введите корректный email');
        isValid = false;
    } else {
        hideError('register-email-error');
    }

    // Валидация пароля
    if (password.length < 8) {
        showError('register-password-error', 'Пароль должен содержать минимум 8 символов');
        isValid = false;
    } else {
        hideError('register-password-error');
    }

    if (!isValid) return;

    try {
        const response = await fetch('/users/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name,
                email,
                password
            })
        });

        if (response.ok) {
            // После успешной регистрации сразу логиним пользователя
            const fingerprint = generateFingerprint();
            const loginResponse = await fetch('/auth/login', {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email,
                    password,
                    fingerprint
                })
            });

            if (loginResponse.ok) {
                const data = await loginResponse.json();
                if (authState.setToken(data.accessToken)) {
                    updateAuthUI(true);
                    document.getElementById('register-modal').style.display = 'none';
                }
            } else {
                document.getElementById('register-error').textContent = 'Регистрация прошла успешно, но вход не удался. Пожалуйста, войдите вручную.';
                document.getElementById('register-error').style.display = 'block';
            }
        } else {
            const errorData = await response.json();
            document.getElementById('register-error').textContent = errorData.message || 'Ошибка при регистрации';
            document.getElementById('register-error').style.display = 'block';
        }
    } catch (error) {
        console.error('Ошибка при регистрации:', error);
        document.getElementById('register-error').textContent = 'Ошибка соединения с сервером';
        document.getElementById('register-error').style.display = 'block';
    }
});

async function loadProfileData() {
    try {
        const response = await authFetch('/users/profile');
        if (response.ok) {
            const profileData = await response.json();
            displayProfileData(profileData);
            return true;
        } else {
            console.error('Ошибка загрузки профиля');
            return false;
        }
    } catch (error) {
        console.error('Ошибка:', error);
        return false;
    }
}

// Отображение данных профиля
function displayProfileData(data) {
    document.getElementById('profile-name').textContent = data.name || 'Не указано';
    document.getElementById('profile-email').textContent = data.email;
    document.getElementById('profile-phone').textContent = data.phone || 'Не указано';
    document.getElementById('profile-balance').textContent = data.balance !== undefined ?
        `${data.balance.toFixed(2)} ₽` : '0.00 ₽';

    // Заполняем форму редактирования
    document.getElementById('edit-name').value = data.name || '';
    document.getElementById('edit-email').value = data.email;
    document.getElementById('edit-phone').value = data.phone || '';
}

// Обработчик кнопки редактирования профиля
document.getElementById('edit-profile-btn').addEventListener('click', () => {
    document.getElementById('edit-profile-modal').style.display = 'flex';
    document.getElementById('edit-profile-error').style.display = 'none';
});

// Закрытие модального окна редактирования
document.getElementById('close-edit-profile').addEventListener('click', () => {
    document.getElementById('edit-profile-modal').style.display = 'none';
});

// Обработка формы редактирования профиля
document.getElementById('edit-profile-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const name = document.getElementById('edit-name').value;
    const email = document.getElementById('edit-email').value;
    const phone = document.getElementById('edit-phone').value;

    // Валидация
    let isValid = true;

    if (!validateEmail(email)) {
        showError('edit-email-error', 'Введите корректный email');
        isValid = false;
    } else {
        hideError('edit-email-error');
    }

    if (!isValid) return;

    try {
        const response = await authFetch('/users/profile/edit', {
            method: 'PATCH',
            body: JSON.stringify({
                name,
                email,
                phone
            })
        });

        if (response.ok) {
            document.getElementById('profile-name').textContent = name || 'Не указано';
            document.getElementById('profile-email').textContent = email;
            document.getElementById('profile-phone').textContent = phone || 'Не указано';
            document.getElementById('edit-profile-modal').style.display = 'none';
        } else {
            const errorData = await response.json();
            console.log(errorData.message);
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
});

const predefinedAmenities = [
    'Wi-Fi', 'Кондиционер', 'Отопление', 'Кухня', 'Стиральная машина',
    'Телевизор', 'Фен', 'Бассейн', 'Парковка', 'Джакузи',
    'Завтрак', 'Рабочая зона', 'Камин', 'Утюг', 'Микроволновка'
];

// Загрузка списка недвижимости пользователя
async function loadOwnProperties() {
    try {
        const response = await authFetch('/properties/own');
        if (response.ok) {
            const properties = await response.json();
            displayProperties(properties);
        } else {
            console.error('Ошибка загрузки недвижимости');
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
}

// Отображение списка недвижимости
function displayProperties(properties) {
    const container = document.getElementById('properties-container');
    container.innerHTML = '';

    if (properties.length === 0) {
        container.innerHTML = '<p>У вас пока нет объектов недвижимости</p>';
        return;
    }

    properties.forEach(property => {
        const propertyElement = document.createElement('div');
        propertyElement.className = 'property-card';
        propertyElement.innerHTML = `
                <h4>${property.title}</h4>
                <p>${property.description || 'Без описания'}</p>
                <button class="edit-property-btn" data-id="${property.id}">Редактировать</button>
            `;
        container.appendChild(propertyElement);
    });
}

// Инициализация формы (добавление чекбоксов удобств)
function initPropertyForm() {
    const container = document.getElementById('amenities-container');
    if (isAmenitiesInited){
        return;
    }
    predefinedAmenities.forEach(amenity => {
        const amenityItem = document.createElement('div');
        amenityItem.className = 'amenity-item';
        amenityItem.innerHTML = `
                <input type="checkbox" id="amenity-${amenity}" name="amenities" value="${amenity}">
                <label for="amenity-${amenity}">${amenity}</label>
            `;
        container.appendChild(amenityItem);
    });
    isAmenitiesInited = true;
}

// Обработка формы создания недвижимости
document.getElementById('create-property-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    // Собираем данные формы
    const formData = new FormData();
    formData.append('Title', document.getElementById('property-title').value);
    formData.append('Description', document.getElementById('property-description').value);
    formData.append('PricePerDay', document.getElementById('property-price').value);
    formData.append('MaxGuests', document.getElementById('property-guests').value);
    formData.append('Bedrooms', document.getElementById('property-bedrooms').value);
    formData.append('PetsAllowed', document.getElementById('property-pets').checked);

// Удобства
    const selectedAmenities = Array.from(document.querySelectorAll('input[name="amenities"]:checked'))
        .map(el => el.value);

    selectedAmenities.forEach((amenity, index) => {
        formData.append(`Amenities[${index}].Name`, amenity);
    });

// Местоположение
    formData.append('Location.Country', document.getElementById('property-country').value);
    formData.append('Location.City', document.getElementById('property-city').value);
    formData.append('Location.Address', document.getElementById('property-address').value);
    formData.append('Location.Street', document.getElementById('property-street').value);
    formData.append('Location.House', document.getElementById('property-house').value);
    formData.append('Location.District', document.getElementById('property-district').value);
    formData.append('Location.Latitude', document.getElementById('property-latitude').value || 0);
    formData.append('Location.Longitude', document.getElementById('property-longitude').value || 0);

// 📷 Главная фотография — под ключом mainFile
    const mainImage = document.getElementById('property-main-image').files[0];
    if (mainImage) {
        formData.append('mainFile', mainImage);
    }

// 📁 Остальные фото — под ключом files
    const additionalImages = document.getElementById('property-images').files;
    for (let i = 0; i < additionalImages.length; i++) {
        formData.append('files', additionalImages[i]);
    }


    // Валидация (можно добавить более сложную)
    /*let isValid = true;
    const requiredFields = [
        'property-title', 'property-description', 'property-price',
        'property-guests', 'property-bedrooms', 'property-country',
        'property-city', 'property-address', 'property-house',
        'property-main-image'
    ];

    requiredFields.forEach(fieldId => {
        const field = document.getElementById(fieldId);
        if (!field.value) {
            showError(`${fieldId}-error`, 'Это поле обязательно');
            isValid = false;
        } else {
            hideError(`${fieldId.split('-')[1]}-error`);
        }
    });

    if (!isValid) return;*/

    try {
        const response = await authFetch('/properties/create', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            alert('Объект успешно добавлен!');
            document.getElementById('create-property-form').reset();
            await loadOwnProperties(); // Обновляем список
        } else {
            const errorData = await response.text();
            alert(`Ошибка: ${errorData || 'Не удалось добавить объект'}`);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        alert('Ошибка соединения с сервером');
    }
});

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', async () => {
    // При загрузке страницы пытаемся обновить токен
    try {
        const refreshed = await refreshToken();
        if (!refreshed) {
            updateAuthUI(false);
        }
    } catch (error) {
        console.error('Ошибка при инициализации:', error);
        updateAuthUI(false);
    }

    // Базовый SPA функционал
    document.querySelectorAll('nav a').forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const page = this.getAttribute('data-page');

            // Проверка авторизации для защищённых страниц
            if (['profile', 'bookings', 'favorites', 'rent-out'].includes(page) && !authState.isTokenValid()) {
                document.getElementById('login-modal').style.display = 'flex';
                return;
            }
            document.querySelectorAll('#main-content,#profile-content, #bookings-content, #favorites-content, #rent-out-content')
                .forEach(el => el.style.display = 'none');
            if (page === 'profile') {
                document.getElementById('profile-content').style.display = 'block';
                loadProfileData();
            } else if (page === 'rent-out') {
                document.getElementById('rent-out-content').style.display = 'block';
                loadOwnProperties();
                initPropertyForm();
            }else if (page === 'favorites') {
                document.getElementById('main-content').style.display = 'block';
                loadFavorites();
            }
        });
    });
});
// Обработка формы поиска недвижимости
document.getElementById('property-search-form').addEventListener('submit', async function(e) {
    e.preventDefault();

    const city = document.getElementById('search-city').value;
    const startDate = document.getElementById('start-date').value;
    const endDate = document.getElementById('end-date').value;
    const adults = parseInt(document.querySelector('.counter-value[data-type="adults"]').textContent);
    const children = parseInt(document.querySelector('.counter-value[data-type="children"]').textContent);
    const hasPets = document.getElementById('has-pets').checked;

    // Валидация дат
    if (new Date(startDate) >= new Date(endDate)) {
        alert('Дата отъезда должна быть позже даты заезда');
        return;
    }

    try {
        const response = await fetch('/properties/search', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                city,
                startDate,
                endDate,
                adults,
                children,
                hasPets
            })
        });

        const resultsContainer = document.getElementById('search-results');
        resultsContainer.innerHTML = '';

        if (response.status === 204) {
            resultsContainer.innerHTML = '<div class="no-results">Ничего не найдено. Попробуйте изменить параметры поиска.</div>';
            return;
        }

        if (!response.ok) {
            throw new Error('Ошибка поиска');
        }

        const properties = await response.json();
        displaySearchResults(properties);
    } catch (error) {
        console.error('Ошибка при поиске:', error);
        document.getElementById('search-results').innerHTML = '<div class="no-results">Произошла ошибка при поиске. Пожалуйста, попробуйте позже.</div>';
    }
});

// Отображение результатов поиска
function displaySearchResults(properties) {
    const resultsContainer = document.getElementById('search-results');
    resultsContainer.innerHTML = '';

    if (properties.length === 0) {
        resultsContainer.innerHTML = '<div class="no-results">Ничего не найдено. Попробуйте изменить параметры поиска.</div>';
        return;
    }

    properties.forEach(property => {
        const propertyElement = createPropertyCard(property);
        resultsContainer.appendChild(propertyElement);
    });
}

// Рендеринг звезд рейтинга
function renderRatingStars(rating) {
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;
    let starsHtml = '';

    for (let i = 1; i <= 5; i++) {
        if (i <= fullStars) {
            starsHtml += '★';
        } else if (i === fullStars + 1 && hasHalfStar) {
            starsHtml += '½';
        } else {
            starsHtml += '☆';
        }
    }

    return starsHtml;
}

// Обработка кликов по примерным городам
document.querySelectorAll('.example-item').forEach(item => {
    item.addEventListener('click', function() {
        document.getElementById('search-city').value = this.textContent;
    });
});

// Обработка выбора количества гостей
document.addEventListener('DOMContentLoaded', function() {
    const guestsSelector = document.getElementById('guests-selector');
    const guestsSummary = document.getElementById('guests-summary');

    let adults = 1;
    let children = 0;

    // Открытие/закрытие выпадающего списка
    guestsSummary.addEventListener('click', function() {
        guestsSelector.classList.toggle('active');
    });

    // Закрытие при клике вне области
    document.addEventListener('click', function(e) {
        if (!guestsSelector.contains(e.target)) {
            guestsSelector.classList.remove('active');
        }
    });

    // Обработка нажатий на кнопки +/-
    document.querySelectorAll('.counter-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const type = this.dataset.type;
            const isPlus = this.classList.contains('plus');
            const counter = document.querySelector(`.counter-value[data-type="${type}"]`);

            if (type === 'adults') {
                adults = Math.max(1, adults + (isPlus ? 1 : -1));
                counter.textContent = adults;
            } else if (type === 'children') {
                children = Math.max(0, children + (isPlus ? 1 : -1));
                counter.textContent = children;
            }

            updateGuestsSummary();
        });
    });

    function updateGuestsSummary() {
        let summary = '';

        if (adults === 1) {
            summary += '1 взрослый';
        } else {
            summary += `${adults} взрослых`;
        }

        if (children > 0) {
            if (children === 1) {
                summary += ', 1 ребенок';
            } else if (children < 5) {
                summary += `, ${children} ребенка`;
            } else {
                summary += `, ${children} детей`;
            }
        } else {
            summary += ', без детей';
        }

        guestsSummary.value = summary;
    }

    // Установка текущей даты и даты +1 день по умолчанию
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);

    document.getElementById('start-date').valueAsDate = today;
    document.getElementById('end-date').valueAsDate = tomorrow;
});
// Обработчик кликов по иконке избранного
// Обработчик кликов по иконке избранного
document.addEventListener('click', async function(e) {
    if (e.target.classList.contains('favorite-icon')) {
        if (!authState.isTokenValid()) {
            document.getElementById('login-modal').style.display = 'flex';
            return;
        }

        const propertyId = e.target.getAttribute('data-id');
        const isFavorite = e.target.classList.contains('active');

        try {
            const endpoint = isFavorite ?
                `/properties/dislike/${propertyId}` :
                `/properties/like/${propertyId}`;

            const response = await authFetch(endpoint, {
                method: 'PATCH'
            });

            if (response.ok) {
                e.target.classList.toggle('active');
            } else {
                console.error('Ошибка при обновлении избранного');
            }
        } catch (error) {
            console.error('Ошибка:', error);
        }
    }
});

// Функция для загрузки избранных объектов
async function loadFavorites() {
    try {
        const response = await authFetch('/properties/favorites');

        if (response.ok) {
            const favorites = await response.json();
            displayFavorites(favorites);
        } else {
            console.error('Ошибка загрузки избранного');
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
}

// Отображение избранных объектов
function displayFavorites(properties) {
    const mainContent = document.getElementById('main-content');
    mainContent.innerHTML = `
        <h1>Избранное</h1>
        <div class="subtitle">Ваши понравившиеся варианты размещения</div>
        <div id="favorites-results" class="search-results"></div>
    `;

    const resultsContainer = document.getElementById('favorites-results');

    if (properties.length === 0) {
        resultsContainer.innerHTML = '<div class="no-results">У вас пока нет избранных объектов</div>';
        return;
    }

    properties.forEach(property => {
        const propertyElement = createPropertyCard(property, true);
        resultsContainer.appendChild(propertyElement);
    });
}

// Создание карточки недвижимости (общая функция для поиска и избранного)
function createPropertyCard(property, isFavoritePage = false) {
    const propertyElement = document.createElement('div');
    propertyElement.className = 'property-card';
    propertyElement.id = `property-${property.id}`;

    const pricePerDay = property.pricePerDay.toLocaleString('ru-RU');

    propertyElement.innerHTML = `
        <div class="property-image-container">
            <img src="data:${property.mainImage.mimeType};base64,${property.mainImage.contentBase64}" 
                 alt="${property.title}" class="property-image">
            <div class="favorite-icon ${property.isFavorite ? 'active' : ''}" 
                 data-id="${property.id}">♥</div>
        </div>
        <div class="property-info">
            <h3 class="property-title">${property.title}</h3>
            <div class="property-location">${property.city}</div>
            <div class="property-price">${pricePerDay} ₽/сутки</div>
            <div class="property-rating">
                ${renderRatingStars(property.averageRating)}
                <span>${property.averageRating.toFixed(1)}</span>
            </div>
            <button class="book-btn" data-id="${property.id}">Забронировать</button>
        </div>
    `;

    return propertyElement;
}
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function showError(elementId, message) {
    const element = document.getElementById(elementId);
    element.textContent = message;
    element.style.display = 'block';
}

function hideError(elementId) {
    document.getElementById(elementId).style.display = 'none';
}