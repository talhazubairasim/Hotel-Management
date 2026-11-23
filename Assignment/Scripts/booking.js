// Booking form: loads available rooms as the dates change, shows a live cost
// estimate, and lets the user pick an existing guest or enter a new one.
(function () {
    'use strict';
    var form = document.getElementById('bookingForm');
    if (!form) return;

    var from = form.querySelector('[name="From"]');
    var to = form.querySelector('[name="To"]');
    var roomSelect = form.querySelector('[name="RoomId"]');
    var estimate = document.getElementById('costEstimate');
    var roomsUrl = form.dataset.roomsUrl;
    var lookupUrl = form.dataset.lookupUrl;
    var currency = form.dataset.currency || '$';
    var lastRooms = [];

    function money(n) { return currency + Number(n).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

    function updateEstimate() {
        var room = lastRooms.find(function (r) { return r.roomId === roomSelect.value; });
        estimate.textContent = room ? 'Estimated cost: ' + money(room.estimate) + ' (' + money(room.charges) + ' per day)' : '';
    }

    function loadRooms() {
        if (!from.value || !to.value || new Date(to.value) <= new Date(from.value)) return;
        var selected = roomSelect.value;
        fetch(roomsUrl + '?from=' + encodeURIComponent(from.value) + '&to=' + encodeURIComponent(to.value), { headers: { Accept: 'application/json' } })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                roomSelect.innerHTML = '';
                var placeholder = new Option(data.success && data.rooms.length ? 'Choose a room' : 'No rooms available for these dates', '');
                roomSelect.add(placeholder);
                lastRooms = data.success ? data.rooms : [];
                lastRooms.forEach(function (r) {
                    var label = r.hotelName + ' — ' + r.roomName + ' (' + r.type + ', floor ' + r.floor + ') — ' + money(r.charges) + '/day';
                    roomSelect.add(new Option(label, r.roomId, false, r.roomId === selected));
                });
                updateEstimate();
            });
    }

    from.addEventListener('change', loadRooms);
    to.addEventListener('change', loadRooms);
    roomSelect.addEventListener('change', updateEstimate);
    loadRooms();

    // Guest picker
    var search = document.getElementById('guestSearch');
    var results = document.getElementById('guestResults');
    var customerId = form.querySelector('[name="CustomerId"]');
    var chosen = document.getElementById('chosenGuest');
    var newGuest = document.getElementById('newGuestFields');
    var timer;

    function choose(id, label) {
        customerId.value = id || '';
        chosen.textContent = label || '';
        chosen.parentElement.hidden = !id;
        newGuest.hidden = !!id;
        results.innerHTML = '';
    }

    document.getElementById('clearGuest').addEventListener('click', function () { choose('', ''); });

    search.addEventListener('input', function () {
        clearTimeout(timer);
        var term = search.value.trim();
        if (term.length < 2) { results.innerHTML = ''; return; }
        timer = setTimeout(function () {
            fetch(lookupUrl + '?term=' + encodeURIComponent(term), { headers: { Accept: 'application/json' } })
                .then(function (r) { return r.json(); })
                .then(function (guests) {
                    results.innerHTML = '';
                    guests.forEach(function (g) {
                        var b = document.createElement('button');
                        b.type = 'button';
                        b.className = 'list-group-item list-group-item-action';
                        b.textContent = g.name + (g.email ? ' · ' + g.email : '') + (g.phone ? ' · ' + g.phone : '');
                        b.addEventListener('click', function () { choose(g.id, b.textContent); search.value = ''; });
                        results.appendChild(b);
                    });
                });
        }, 250);
    });
})();
