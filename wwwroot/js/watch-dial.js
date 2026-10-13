/**
 * TimeVerse Interactive Floating Watch Dial
 * Renders hour markers, connects hands to real-time clock,
 * and adds interactive 3D parallax tilt based on cursor movement.
 */
document.addEventListener('DOMContentLoaded', () => {
    const dialFace = document.querySelector('.tv-dial-face');
    const hourHand = document.querySelector('.tv-hand.hour');
    const minuteHand = document.querySelector('.tv-hand.minute');
    const secondHand = document.querySelector('.tv-hand.second');
    const dialWrapper = document.querySelector('.tv-watch-dial-wrapper');

    if (!dialFace || !hourHand || !minuteHand || !secondHand) {
        return;
    }

    // 1. Generate 12 Hour Markers dynamically around the perimeter
    const radius = 136; // Distance from center to marker
    for (let i = 0; i < 12; i++) {
        const marker = document.createElement('div');
        marker.className = 'tv-marker';
        const isCardinal = (i % 3 === 0);
        if (isCardinal) {
            marker.classList.add('cardinal');
        }

        const angle = i * 30; // 360 / 12 = 30 deg
        marker.style.transform = `rotate(${angle}deg)`;
        dialFace.appendChild(marker);
    }

    // 2. Continuous Real-Time Precision Mechanical Movement
    function updateWatchHands() {
        const now = new Date();
        const ms = now.getMilliseconds();
        const seconds = now.getSeconds() + ms / 1000;
        const minutes = now.getMinutes() + seconds / 60;
        const hours = (now.getHours() % 12) + minutes / 60;

        const secDeg = seconds * 6; // 360 / 60 = 6 deg
        const minDeg = minutes * 6;
        const hourDeg = hours * 30; // 360 / 12 = 30 deg

        secondHand.style.transform = `rotate(${secDeg}deg)`;
        minuteHand.style.transform = `rotate(${minDeg}deg)`;
        hourHand.style.transform = `rotate(${hourDeg}deg)`;

        requestAnimationFrame(updateWatchHands);
    }

    requestAnimationFrame(updateWatchHands);

    // 3. Interactive Mouse Parallax Floating Tilt
    if (dialWrapper) {
        const stage = document.querySelector('.tv-dial-stage');
        if (stage) {
            stage.addEventListener('mousemove', (e) => {
                const rect = stage.getBoundingClientRect();
                const x = e.clientX - rect.left - rect.width / 2;
                const y = e.clientY - rect.top - rect.height / 2;

                const tiltX = -(y / (rect.height / 2)) * 14;
                const tiltY = (x / (rect.width / 2)) * 14;

                dialWrapper.style.transform = `rotateX(${tiltX}deg) rotateY(${tiltY}deg) translateY(-8px)`;
            });

            stage.addEventListener('mouseleave', () => {
                dialWrapper.style.transform = `rotateX(0deg) rotateY(0deg) translateY(0)`;
            });
        }
    }
});
