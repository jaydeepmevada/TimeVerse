/**
 * TimeVerse — Real-Time Analog Clock Implementation
 * Developed for BCA College Project
 * 
 * Features:
 * 1. Uses native JavaScript Date API to read current browser local time.
 * 2. Computes rotational angles for Hour Hand, Minute Hand, and Second Hand.
 * 3. Dynamically generates 12 hour markers (I through XII / Cardinal ticks).
 * 4. Uses requestAnimationFrame for a continuous, smooth sweeping mechanical movement.
 * 5. Displays synchronized digital time & date readout.
 */

(function () {
    'use strict';

    function initTimeVerseClock() {
        const dial = document.getElementById('tvRealTimeDial');
        const hourHand = document.getElementById('tvHourHand');
        const minuteHand = document.getElementById('tvMinuteHand');
        const secondHand = document.getElementById('tvSecondHand');
        const digitalTime = document.getElementById('tvDigitalTime');
        const digitalDate = document.getElementById('tvDigitalDate');

        if (!dial || !hourHand || !minuteHand || !secondHand) {
            return;
        }

        // Dynamically create 12 hour markers around the clock face
        // 360 degrees / 12 hours = 30 degrees per hour
        const markersContainer = document.getElementById('tvDialMarkers');
        if (markersContainer && markersContainer.children.length === 0) {
            for (let i = 1; i <= 12; i++) {
                const marker = document.createElement('div');
                marker.className = 'tv-clock-marker';
                if (i % 3 === 0) {
                    marker.classList.add('cardinal'); // 12, 3, 6, 9
                }
                const angle = i * 30;
                marker.style.transform = `rotate(${angle}deg)`;
                markersContainer.appendChild(marker);
            }
        }

        function updateClock() {
            // Read current local time from browser Date API
            const now = new Date();
            const ms = now.getMilliseconds();
            const sec = now.getSeconds();
            const min = now.getMinutes();
            const hr = now.getHours();

            // Calculate precise angles for smooth mechanical sweep
            // Second hand: 360 deg / 60 sec = 6 deg per second (+ fractional ms for smooth sweep)
            const secondDeg = (sec + ms / 1000) * 6;

            // Minute hand: 360 deg / 60 min = 6 deg per minute (+ fractional seconds)
            const minuteDeg = (min + sec / 60) * 6;

            // Hour hand: 360 deg / 12 hr = 30 deg per hour (+ fractional minutes)
            const hourDeg = ((hr % 12) + min / 60) * 30;

            // Apply CSS rotation transform
            secondHand.style.transform = `rotate(${secondDeg}deg)`;
            minuteHand.style.transform = `rotate(${minuteDeg}deg)`;
            hourHand.style.transform = `rotate(${hourDeg}deg)`;

            // Update digital time readout (HH:MM:SS AM/PM)
            if (digitalTime) {
                const displayHr = hr % 12 === 0 ? 12 : hr % 12;
                const paddedMin = String(min).padStart(2, '0');
                const paddedSec = String(sec).padStart(2, '0');
                const amPm = hr >= 12 ? 'PM' : 'AM';
                digitalTime.textContent = `${displayHr}:${paddedMin}:${paddedSec} ${amPm}`;
            }

            // Update digital date readout
            if (digitalDate) {
                const options = { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' };
                digitalDate.textContent = now.toLocaleDateString(undefined, options);
            }

            // Continuous animation loop without page reload
            requestAnimationFrame(updateClock);
        }

        // Start clock movement loop
        requestAnimationFrame(updateClock);
    }

    // Initialize when DOM is fully loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTimeVerseClock);
    } else {
        initTimeVerseClock();
    }
})();
