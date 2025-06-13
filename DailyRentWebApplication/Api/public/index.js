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
            }),
            headers: {
                'Content-Type': 'application/json'
            }
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
            showNotification('Объект успешно добавлен!');
            document.getElementById('create-property-form').reset();
            await loadOwnProperties(); // Обновляем список
        } else {
            const errorData = await response.text();
            showNotification(`Ошибка: ${errorData || 'Не удалось добавить объект'}`);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showNotification('Ошибка соединения с сервером');
    }
});

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', async () => {
    // Состояние приложения
    const appState = {
        currentView: 'main',
        currentPropertyId: null,
        searchResults: []
    };
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
            showNotification('Дата отъезда должна быть позже даты заезда');
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
    // Создание карточки недвижимости (общая функция для поиска и избранного)
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
            <button class="book-btn" id="search-${property.id}" data-id="${property.id}">Забронировать</button>
        </div>
    `;
        const bookBtn = propertyElement.querySelector('.book-btn');
        bookBtn.addEventListener('click', async function(e) {
            e.stopPropagation(); // Останавливаем всплытие события
            if (!authState.isTokenValid()) {
                document.getElementById('login-modal').style.display = 'flex';
                return;
            }
            // Получаем данные из формы поиска
            const checkInDate = document.getElementById('start-date').value;
            const checkOutDate = document.getElementById('end-date').value;
            const adultsCount = parseInt(document.querySelector('.counter-value[data-type="adults"]').textContent);
            const childrenCount = parseInt(document.querySelector('.counter-value[data-type="children"]').textContent);
            const hasPets = document.getElementById('has-pets').checked;

            // Создаем DTO
            const bookingRequest = {
                PropertyId: property.id,
                CheckInDate: checkInDate,
                CheckOutDate: checkOutDate,
                AdultsCount: adultsCount,
                ChildrenCount: childrenCount,
                HasPets: hasPets
            };

            try {
                const response = await authFetch('/bookings/create', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(bookingRequest)
                });

                if (response.ok) {
                    showNotification('Бронирование успешно создано!');
                } else {
                    const error = await response.json();
                    showNotification(`Ошибка: ${error.message || 'Не удалось создать бронирование'}`);
                }
            } catch (error) {
                console.error('Ошибка:', error);
                showNotification('Ошибка соединения с сервером');
            }
        });

        // Обработчик для остальной части карточки
        propertyElement.addEventListener('click', function() {
            showView('property-details', property.id);
        });

        return propertyElement;
    }
    // Инициализация
    try {
        const refreshed = await refreshToken();
        if (!refreshed) updateAuthUI(false);
    } catch (error) {
        console.error('Ошибка при инициализации:', error);
        updateAuthUI(false);
    }

    // Основная функция отображения контента
    function showView(viewName, propertyId = null) {
        // Скрываем все разделы
        document.querySelectorAll('#main-content,#profile-content, #bookings-content, #favorites-content, #rent-out-content')
            .forEach(el => el.style.display = 'none');

        // Обновляем состояние
        appState.currentView = viewName;
        appState.currentPropertyId = propertyId;

        // Показываем нужный раздел
        if (viewName === 'property-details' && propertyId) {
            document.getElementById('main-content').style.display = 'block';
            loadPropertyDetails(propertyId);
        } else if (viewName === 'profile') {
            document.getElementById('profile-content').style.display = 'block';
            loadProfileData();
        } else if (viewName === 'rent-out') {
            document.getElementById('rent-out-content').style.display = 'block';
            loadOwnProperties();
            initPropertyForm();
        } else if (viewName === 'favorites') {
            document.getElementById('main-content').style.display = 'block';
            loadFavorites();
        } else if (viewName === 'bookings') {
            document.getElementById('bookings-content').style.display = 'block';
            loadUserBookings();
        }else {
            document.getElementById('main-content').style.display = 'block';
            // Можно добавить загрузку главной страницы
        }
    }
    document.addEventListener('click', async (e) => {
        // 1. Обработка кликов по сердечку (избранное)
        if (e.target.closest('.favorite-icon')) {
            e.preventDefault();
            e.stopPropagation(); // Предотвращаем всплытие события

            if (!authState.isTokenValid()) {
                document.getElementById('login-modal').style.display = 'flex';
                return;
            }

            const icon = e.target.closest('.favorite-icon');
            const propertyId = icon.getAttribute('data-id');
            const isFavorite = icon.classList.contains('active');

            try {
                const endpoint = isFavorite ?
                    `/properties/dislike/${propertyId}` :
                    `/properties/like/${propertyId}`;

                const response = await authFetch(endpoint, { method: 'PATCH' });

                if (response.ok) {
                    icon.classList.toggle('active');

                    // Если мы на странице избранного - обновляем список
                    if (appState.currentView === 'favorites') {
                        loadFavorites();
                    }
                }
            } catch (error) {
                console.error('Ошибка:', error);
            }
            return;
        }

        
        

        // 3. Обработка кнопки "Назад"
        if (e.target.id === 'back-to-results') {
            showView('main');
        }
    });

    // Обработчик навигации
    document.querySelectorAll('nav a').forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const page = this.getAttribute('data-page');

            // Проверка авторизации
            if (['profile', 'bookings', 'favorites', 'rent-out'].includes(page) && !authState.isTokenValid()) {
                document.getElementById('login-modal').style.display = 'flex';
                return;
            }

            showView(page);
        });
    });
    

    // Функция загрузки деталей помещения
    async function loadPropertyDetails(propertyId) {
        try {
            const response = await authFetch(`/properties/details/${propertyId}`);
            if (!response.ok) throw new Error('Ошибка загрузки');

            const property = await response.json();
            renderPropertyDetails(property);
        } catch (error) {
            console.error('Ошибка:', error);
            document.getElementById('main-content').innerHTML = `
                <div class="error-message">
                    Не удалось загрузить информацию о помещении
                    <button onclick="showView('main')">Вернуться назад</button>
                </div>
            `;
        }
    }

    // Функция отрисовки деталей помещения
    function renderPropertyDetails(property) {
        const mainContent = document.getElementById('main-content');
        mainContent.innerHTML = `
        <button id="back-to-results" class="back-btn">← Назад к результатам</button>
        <div class="property-details">
            <div class="property-gallery" id="property-gallery">
                ${property.images.map((img, index) => `
                    <img src="data:${img.mimeType};base64,${img.contentBase64}" 
                         class="${img.isMain ? 'main-image' : 'thumbnail'}" 
                         alt="${property.title}"
                         data-index="${index}">
                `).join('')}
            </div>
            
            <div class="property-header">
                <h1>${property.title}</h1>
                <div class="property-price">${property.pricePerDay.toLocaleString('ru-RU')} ₽/сутки</div>
            </div>
            
            <div class="property-meta">
                <span>${property.maxGuests} гостя</span>
                <span>${property.bedrooms} спальни</span>
                <span>${property.petsAllowed ? 'Можно с животными' : 'Без животных'}</span>
            </div>
            
            <div class="property-section">
                <h2>Описание</h2>
                <p>${property.description}</p>
            </div>
            
            <div class="property-section">
                <h2>Удобства</h2>
                <div class="amenities-grid">
                    ${property.amenities.map(a => `
                        <div class="amenity-item">
                            <span class="amenity-icon">✓</span>
                            <span>${a.name}</span>
                        </div>
                    `).join('')}
                </div>
            </div>
            
            <button class="book-btn">Забронировать</button>
        </div>
    `;

        // Добавляем обработчик клика по кнопке бронирования
        mainContent.querySelector('.book-btn').addEventListener('click', async () => {
            if (!authState.isTokenValid()) {
                document.getElementById('login-modal').style.display = 'flex';
                return;
            }
            // Получаем данные из формы поиска
            const checkInDate = document.getElementById('start-date').value;
            const checkOutDate = document.getElementById('end-date').value;
            const adultsCount = parseInt(document.querySelector('.counter-value[data-type="adults"]').textContent);
            const childrenCount = parseInt(document.querySelector('.counter-value[data-type="children"]').textContent);
            const hasPets = document.getElementById('has-pets').checked;

            // Создаем DTO
            const bookingRequest = {
                PropertyId: propertyId,
                CheckInDate: checkInDate,
                CheckOutDate: checkOutDate,
                AdultsCount: adultsCount,
                ChildrenCount: childrenCount,
                HasPets: hasPets
            };

            try {
                const response = await authFetch('/bookings/create', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(bookingRequest)
                });

                if (response.ok) {
                    showNotification('Бронирование успешно создано!');
                } else {
                    const error = await response.json();
                    showNotification(`Ошибка: ${error.message || 'Не удалось создать бронирование'}`);
                }
            } catch (error) {
                console.error('Ошибка:', error);
                showNotification('Ошибка соединения с сервером');
            }
        });

        // Обработчик кнопки "Назад"
        document.getElementById('back-to-results').addEventListener('click', () => {
            showView('main');
        });

        // Обработчик переключения изображений
        const gallery = document.getElementById('property-gallery');
        gallery.addEventListener('click', (e) => {
            const clickedImg = e.target.closest('img');
            if (!clickedImg || clickedImg.classList.contains('main-image')) return;

            const mainImage = gallery.querySelector('.main-image');
            const thumbnails = gallery.querySelectorAll('.thumbnail');

            // Меняем местами main и thumbnail
            mainImage.classList.remove('main-image');
            mainImage.classList.add('thumbnail');

            clickedImg.classList.remove('thumbnail');
            clickedImg.classList.add('main-image');

            // Перемещаем main изображение в начало
            gallery.insertBefore(clickedImg, gallery.firstChild);
        });
    }

    // Инициализация начального вида
    showView('main');
});
// Обработка формы поиска недвижимости



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
// Загрузка бронирований пользователя
async function loadUserBookings() {
    try {
        const response = await authFetch('/bookings/all');
        if (response.ok) {
            const bookings = await response.json();

            // Для каждого бронирования загружаем заявку на компенсацию, если она есть
            const bookingsWithCompensation = await Promise.all(
                bookings.map(async booking => {
                    if (booking.isPaid) {
                        const compResponse = await authFetch(`/compensation-requests/booking/${booking.id}`);
                        if (compResponse.ok) {
                            booking.compensationRequest = await compResponse.json();
                        }
                        else{
                            booking.compensationRequest = null;
                        }
                    }
                    else{
                        booking.compensationRequest = null;
                    }
                    return booking;
                })
            );

            displayBookings(bookingsWithCompensation);
        } else {
            console.error('Ошибка загрузки бронирований');
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
}

// Отображение списка бронирований
// Отображение списка бронирований
function displayBookings(bookings) {
    const bookingsContent = document.getElementById('bookings-content');
    bookingsContent.innerHTML = `
        <h1>Мои бронирования</h1>
        <div class="bookings-list">
            ${bookings.length > 0 ?
        bookings.map(booking => createBookingCard(booking)).join('') :
        '<p class="no-bookings">У вас пока нет бронирований</p>'
    }
        </div>
    `;

    // Добавляем обработчики для кнопок отмены
    document.querySelectorAll('.cancel-booking-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            const bookingId = e.target.dataset.bookingId;
            if (showConfirmation('Вы уверены, что хотите отменить бронирование?')) {
                try {
                    const response = await authFetch(`/bookings/cancel/${bookingId}`, {
                        method: 'PATCH'
                    });

                    if (response.ok) {
                        // Обновляем статус бронирования
                        const bookingCard = e.target.closest('.booking-card');
                        bookingCard.querySelector('.booking-status').textContent = 'Отменено';
                        bookingCard.querySelector('.cancel-booking-btn').style.display = 'none';
                        showNotification('Бронирование отменено');
                    } else {
                        const error = await response.json();
                        showNotification(`Ошибка: ${error.message || 'Не удалось отменить бронирование'}`);
                    }
                } catch (error) {
                    console.error('Ошибка:', error);
                    showNotification('Ошибка соединения с сервером');
                }
            }
        });
    });

    // Добавляем обработчики для кнопок оплаты
    document.querySelectorAll('.pay-booking-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            const bookingId = e.target.dataset.bookingId;
            await processPayment(bookingId);
        });
    });

    document.querySelectorAll('.create-compensation-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const bookingId = e.target.dataset.bookingId;
            document.getElementById('compensation-booking-id').value = bookingId;
            document.getElementById('compensation-modal').style.display = 'flex';
        });
    });

    document.querySelectorAll('.delete-request-btn').forEach(btn => {
        btn.addEventListener('click', handleDeleteCompensationRequest);
    });

    // Закрытие модального окна
    document.getElementById('close-compensation').addEventListener('click', () => {
        document.getElementById('compensation-modal').style.display = 'none';
    });

    // Обработка формы компенсации
    document.getElementById('compensation-form').addEventListener('submit', handleCompensationSubmit);
}
async function handleCompensationSubmit(e) {
    e.preventDefault();

    const bookingId = document.getElementById('compensation-booking-id').value;
    const description = document.getElementById('compensation-description').value;
    const amount = document.getElementById('compensation-amount').value;
    const photos = document.getElementById('compensation-photos').files;

    // Валидация
    let isValid = true;

    if (description.length < 20) {
        showError('compensation-description-error', 'Описание должно содержать минимум 20 символов');
        isValid = false;
    } else {
        hideError('compensation-description-error');
    }

    if (!amount || parseFloat(amount) <= 0) {
        showError('compensation-amount-error', 'Введите корректную сумму');
        isValid = false;
    } else {
        hideError('compensation-amount-error');
    }

    if (!isValid) return;

    try {
        const formData = new FormData();
        formData.append('Description', description);
        formData.append('RequestedAmount', amount);
        formData.append('BookingId', bookingId);

        for (let i = 0; i < photos.length; i++) {
            formData.append('ProofPhotos', photos[i]);
        }

        const response = await authFetch('/compensation-requests', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            showNotification('Заявка на компенсацию успешно создана!');
            document.getElementById('compensation-modal').style.display = 'none';
            document.getElementById('compensation-form').reset();
            await loadUserBookings();
        } else {
            const error = await response.json();
            document.getElementById('compensation-error').textContent = error.message || 'Ошибка при создании заявки';
            document.getElementById('compensation-error').style.display = 'block';
        }
    } catch (error) {
        console.error('Ошибка:', error);
        document.getElementById('compensation-error').textContent = 'Ошибка соединения с сервером';
        document.getElementById('compensation-error').style.display = 'block';
    }
}

async function handleDeleteCompensationRequest(e) {
    const requestId = e.target.dataset.requestId;

    if (showConfirmation('Вы уверены, что хотите удалить эту заявку?')) {
        try {
            const response = await authFetch(`/compensation-requests/${requestId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                showNotification('Заявка успешно удалена');
                await loadUserBookings();
            } else {
                const error = await response.json();
                showNotification(`Ошибка: ${error.message || 'Не удалось удалить заявку'}`);
            }
        } catch (error) {
            console.error('Ошибка:', error);
            showNotification('Ошибка соединения с сервером');
        }
    }
}
// Создание карточки бронирования
// Создание карточки бронирования
function createBookingCard(booking) {
    const checkInDate = new Date(booking.checkInDate).toLocaleDateString('ru-RU');
    const checkOutDate = new Date(booking.checkOutDate).toLocaleDateString('ru-RU');
    const createdDate = new Date(booking.createdDate).toLocaleDateString('ru-RU');
    const totalPrice = booking.totalPrice.toLocaleString('ru-RU');
    const isPaid = booking.isPaid || false;
    const isPayProcess = booking.isPayProcess || false;
    const hasCompensationRequest = booking.compensationRequest !== null;
    return `
        <div class="booking-card" data-booking-id="${booking.id}">
            <div class="booking-header">
                <h3>${booking.propertyTitle}</h3>
                <span class="booking-city">${booking.propertyCity}</span>
            </div>
            <div class="booking-dates">
                <span>Заезд: ${checkInDate}</span>
                <span>Выезд: ${checkOutDate}</span>
            </div>
            <div class="booking-meta">
                <span>Гости: ${booking.adultsCount} взрослых, ${booking.childrenCount} детей</span>
                <span>Животные: ${booking.hasPets ? 'Да' : 'Нет'}</span>
            </div>
            <div class="booking-footer">
                <div class="booking-price">${totalPrice} ₽</div>
                <div class="booking-status ${booking.status.toLowerCase()} ${isPaid ? 'paid' : ''}">
                    ${getStatusText(booking.status)}${isPaid ? ' (Оплачено)' : ''}
                </div>
            </div>
            <div class="booking-actions">
                ${booking.status === 'Approved' && !isPayProcess ?
        `<button class="pay-booking-btn" data-booking-id="${booking.id}">Оплатить</button>` : ''
    }
                ${isPaid && !hasCompensationRequest ?
        `<button class="create-compensation-btn" data-booking-id="${booking.id}">Создать заявку на компенсацию</button>` : ''
    }
                ${booking.status !== 'Completed' && booking.status !== 'Rejected' && booking.status !== 'Cancelled' && !isPayProcess ?
        `<button class="cancel-booking-btn" data-booking-id="${booking.id}">Отменить</button>` : ''
    }
            </div>
            ${hasCompensationRequest ? renderCompensationRequest(booking.compensationRequest) : ''}
            <div class="booking-created">Создано: ${createdDate}</div>
        </div>
    `;
}
function renderCompensationRequest(request) {
    return `
        <div class="compensation-request" data-request-id="${request.id}">
            <h4>Заявка на компенсацию</h4>
            <div class="request-details">
                <p><strong>Описание:</strong> ${request.description}</p>
                <p><strong>Запрошенная сумма:</strong> ${request.requestedAmount.toLocaleString('ru-RU')} ₽</p>
                ${request.approvedAmount !== null ?
        `<p><strong>Одобренная сумма:</strong> ${request.approvedAmount.toLocaleString('ru-RU')} ₽</p>` : ''
    }
                <p><strong>Статус:</strong> <span class="status-${request.status.toLowerCase()}">${getCompensationStatusText(request.status)}</span></p>
                ${request.proofPhotos && request.proofPhotos.length > 0 ? `
                    <div class="proof-photos">
                        <strong>Доказательства:</strong>
                        <div class="photos-grid">
                            ${request.proofPhotos.map(photo => `
                                <img src="data:${photo.mimeType};base64,${photo.contentBase64}" alt="Доказательство" class="proof-photo">
                            `).join('')}
                        </div>
                    </div>
                ` : ''}
            </div>
            ${request.status === 'Pending' ? `
                <div class="request-actions">
                    <button class="delete-request-btn" data-request-id="${request.id}">Удалить заявку</button>
                </div>
            ` : ''}
        </div>
    `;
}

function getCompensationStatusText(status) {
    const statusMap = {
        'Pending': 'На рассмотрении',
        'Approved': 'Одобрено',
        'Rejected': 'Отклонено'
    };
    return statusMap[status] || status;
}
// Обновленная функция загрузки собственной недвижимости
async function loadOwnProperties() {
    try {
        // Загружаем список недвижимости пользователя
        const propertiesResponse = await authFetch('/properties/own');
        if (!propertiesResponse.ok) throw new Error('Ошибка загрузки недвижимости');

        const properties = await propertiesResponse.json();

        // Для каждой недвижимости загружаем бронирования с компенсациями
        const propertiesWithBookings = await Promise.all(
            properties.map(async property => {
                try {
                    // Загружаем бронирования для этой недвижимости
                    const bookingsResponse = await authFetch(`/bookings/all/${property.id}`);
                    if (bookingsResponse.ok) {
                        property.bookings = await bookingsResponse.json();

                        // Для каждого бронирования загружаем компенсацию
                        property.bookings = await Promise.all(
                            property.bookings.map(async booking => {
                                if (booking.isPaid) {
                                    const compResponse = await authFetch(`/compensation-requests/booking/${booking.id}`);
                                    if (compResponse.ok) {
                                        booking.compensationRequest = await compResponse.json();
                                    }
                                }
                                return booking;
                            })
                        );
                    } else {
                        property.bookings = [];
                    }
                } catch (error) {
                    console.error(`Ошибка при загрузке бронирований для property ${property.id}:`, error);
                    property.bookings = [];
                }
                return property;
            })
        );

        displayProperties(propertiesWithBookings);
    } catch (error) {
        console.error('Ошибка:', error);
        document.getElementById('properties-container').innerHTML =
            '<p class="error-message">Не удалось загрузить данные. Пожалуйста, попробуйте позже.</p>';
    }
}

// Обновленная функция отображения недвижимости с бронированиями
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
            <div class="property-card-header">
                <h4>${property.title}</h4>
                <p>${property.description || 'Без описания'}</p>
            </div>
            <div class="property-bookings">
                <h5>Заявки на бронирование:</h5>
                ${property.bookings && property.bookings.length > 0 ?
            renderBookingsList(property.bookings) :
            '<p>Нет активных заявок</p>'
        }
            </div>
        `;
        container.appendChild(propertyElement);
    });

    // В функции displayProperties добавьте:
    document.querySelectorAll('.view-request-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            const requestId = e.target.dataset.requestId;
            await showCompensationRequestDetails(requestId);
        });
    });

    async function showCompensationRequestDetails(requestId) {
        try {
            const response = await authFetch(`/compensation-requests/${requestId}`);
            if (response.ok) {
                const request = await response.json();
                renderCompensationRequestDetails(request);
            } else {
                throw new Error('Ошибка загрузки заявки');
            }
        } catch (error) {
            console.error('Ошибка:', error);
            showNotification('Не удалось загрузить данные заявки');
        }
    }

    function renderCompensationRequestDetails(request) {
        const detailsContainer = document.getElementById('compensation-details');
        detailsContainer.innerHTML = `
        <div class="request-details">
            <p><strong>Описание:</strong> ${request.description}</p>
            <p><strong>Запрошенная сумма:</strong> ${request.requestedAmount.toLocaleString('ru-RU')} ₽</p>
            ${request.approvedAmount !== null ?
            `<p><strong>Одобренная сумма:</strong> ${request.approvedAmount.toLocaleString('ru-RU')} ₽</p>` : ''
        }
            <p><strong>Статус:</strong> <span class="status-${request.status.toLowerCase()}">${getCompensationStatusText(request.status)}</span></p>
            ${request.proofPhotos && request.proofPhotos.length > 0 ? `
                <div class="proof-photos">
                    <strong>Доказательства:</strong>
                    <div class="photos-grid">
                        ${request.proofPhotos.map(photo => `
                            <img src="data:${photo.mimeType};base64,${photo.contentBase64}" alt="Доказательство" class="proof-photo">
                        `).join('')}
                    </div>
                </div>
            ` : ''}
        </div>
    `;

        const approveForm = document.getElementById('compensation-approve-form');
        if (request.status === 'Pending') {
            approveForm.style.display = 'block';
            document.getElementById('approve-amount').value = request.requestedAmount;

            // Удаляем старые обработчики
            const oldApproveBtn = document.getElementById('approve-compensation-btn');
            const newApproveBtn = oldApproveBtn.cloneNode(true);
            oldApproveBtn.parentNode.replaceChild(newApproveBtn, oldApproveBtn);

            const oldRejectBtn = document.getElementById('reject-compensation-btn');
            const newRejectBtn = oldRejectBtn.cloneNode(true);
            oldRejectBtn.parentNode.replaceChild(newRejectBtn, oldRejectBtn);

            // Добавляем новые обработчики
            newApproveBtn.addEventListener('click', async () => {
                const amount = document.getElementById('approve-amount').value;
                await processCompensationAction(request.id, 'approve', amount);
            });

            newRejectBtn.addEventListener('click', async () => {
                await processCompensationAction(request.id, 'reject');
            });
        } else {
            approveForm.style.display = 'none';
        }

        document.getElementById('view-compensation-modal').style.display = 'flex';
    }

    async function processCompensationAction(requestId, action, amount = null) {
        try {
            let endpoint = `/compensation-requests/${requestId}/${action}`;
            if (action === 'approve' && amount !== null) {
                endpoint += `/${amount}`;
            }

            const response = await authFetch(endpoint, {
                method: 'PATCH'
            });

            if (response.ok) {
                showNotification(`Заявка успешно ${action === 'approve' ? 'одобрена' : 'отклонена'}`);
                document.getElementById('view-compensation-modal').style.display = 'none';
                await loadOwnProperties(); // Обновляем список
            } else {
                const error = await response.json();
                showNotification(`Ошибка: ${error.message || 'Не удалось выполнить действие'}`);
            }
        } catch (error) {
            console.error('Ошибка:', error);
            showNotification('Ошибка соединения с сервером');
        }
    }

// Закрытие модального окна
    document.getElementById('close-view-compensation').addEventListener('click', () => {
        document.getElementById('view-compensation-modal').style.display = 'none';
    });
}

// Функция для отображения списка бронирований для конкретной недвижимости
function renderBookingsList(bookings) {
    return `
        <div class="bookings-list">
            ${bookings.map(booking => `
                <div class="booking-item" data-booking-id="${booking.id}">
                    <div class="booking-dates">
                        ${formatDate(booking.checkInDate)} - ${formatDate(booking.checkOutDate)}
                    </div>
                    <div class="booking-guests">
                        ${booking.adultsCount} взрослых, ${booking.childrenCount} детей
                        ${booking.hasPets ? ', с животными' : ''}
                    </div>
                    <div class="booking-price">
                        ${booking.totalPrice.toLocaleString('ru-RU')} ₽
                    </div>
                    <div class="booking-status ${booking.status.toLowerCase()}">
                        ${getStatusText(booking.status)}
                    </div>
                    ${booking.compensationRequest ? `
                        <div class="compensation-request" data-request-id="${booking.compensationRequest.id}">
                            <div class="request-status ${booking.compensationRequest.status.toLowerCase()}">
                                Заявка на компенсацию: ${getCompensationStatusText(booking.compensationRequest.status)}
                            </div>
                            <button class="view-request-btn" data-request-id="${booking.compensationRequest.id}">Подробнее</button>
                        </div>
                    ` : ''}
                    ${booking.status === 'Pending' ? `
                        <div class="booking-actions">
                            <button class="approve-booking-btn" data-booking-id="${booking.id}">Принять</button>
                            <button class="reject-booking-btn" data-booking-id="${booking.id}">Отклонить</button>
                        </div>
                    ` : ''}
                </div>
            `).join('')}
        </div>
    `;
}

// Функция для форматирования даты
function formatDate(dateString) {
    const options = { day: 'numeric', month: 'short' };
    return new Date(dateString).toLocaleDateString('ru-RU', options);
}

// Добавляем обработчики для кнопок принятия/отклонения бронирования
document.getElementById('properties-container').addEventListener('click', async (e) => {
    if (e.target.classList.contains('approve-booking-btn')) {
        const bookingId = e.target.dataset.bookingId;
        await processBookingAction(bookingId, 'approve');
    } else if (e.target.classList.contains('reject-booking-btn')) {
        const bookingId = e.target.dataset.bookingId;
        await processBookingAction(bookingId, 'reject');
    }
});

// Функция для обработки действий с бронированием
async function processBookingAction(bookingId, action) {
    try {
        const endpoint = action === 'approve' ?
            `/bookings/approve/${bookingId}` :
            `/bookings/reject/${bookingId}`;

        const response = await authFetch(endpoint, {
            method: 'PATCH'
        });

        if (response.ok) {
            showNotification(`Бронирование успешно ${action === 'approve' ? 'подтверждено' : 'отклонено'}`);
            // Обновляем список недвижимости
            await loadOwnProperties();
        } else {
            const error = await response.json();
            showNotification(`Ошибка: ${error.message || 'Не удалось выполнить действие'}`);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showNotification('Ошибка соединения с сервером');
    }
}
// Функция для обработки оплаты бронирования
async function processPayment(bookingId) {
    try {
        const response = await authFetch(`/bookings/pay/${bookingId}`, {
            method: 'POST'
        });
        
        if (response.ok) {
            window.location.href = (await response.json()).url;
        } else {
            const error = await response.text();
                showNotification(`Ошибка: ${error || 'Не удалось выполнить оплату'}`);
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showNotification('Ошибка соединения с сервером');
    }
}
// Обновленная функция getStatusText для новых статусов
function getStatusText(status) {
    const statusMap = {
        'Pending': 'Ожидает подтверждения',
        'Approved': 'Подтверждено',
        'Cancelled': 'Отменено',
        'Completed': 'Завершено',
        'Rejected': 'Отклонено'
    };
    return statusMap[status] || status;
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

// Добавьте эти функции в ваш JavaScript
function showNotification(message, type = 'success') {
    const notification = document.getElementById('notification');
    const messageElement = document.getElementById('notification-message');

    notification.className = `notification ${type}`;
    messageElement.textContent = message;
    notification.classList.remove('hidden');

    setTimeout(() => {
        notification.classList.add('hidden');
    }, 5000);
}

async function showConfirmation(message, title = 'Подтверждение') {
    return new Promise((resolve) => {
        const modal = document.getElementById('confirmation-modal');
        const titleElement = document.getElementById('confirmation-title');
        const messageElement = document.getElementById('confirmation-message');
        const confirmBtn = document.getElementById('confirm-ok');
        const cancelBtn = document.getElementById('confirm-cancel');

        titleElement.textContent = title;
        messageElement.textContent = message;
        modal.style.display = 'flex';

        const cleanUp = () => {
            confirmBtn.removeEventListener('click', onConfirm);
            cancelBtn.removeEventListener('click', onCancel);
            modal.style.display = 'none';
        };

        const onConfirm = () => {
            cleanUp();
            resolve(true);
        };

        const onCancel = () => {
            cleanUp();
            resolve(false);
        };

        confirmBtn.addEventListener('click', onConfirm);
        cancelBtn.addEventListener('click', onCancel);
    });
}

// Пример замены showNotification на showNotification:
// Было: showNotification('Объект успешно добавлен!');
// Стало: showNotification('Объект успешно добавлен!');

// Пример замены confirm на showConfirmation:
// Было: if (showConfirmation('Вы уверены, что хотите отменить бронирование?')) {...}
// Стало: if (await showConfirmation('Вы уверены, что хотите отменить бронирование?')) {...}