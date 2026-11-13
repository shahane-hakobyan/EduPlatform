// «Իմ վիճակագրությունը»՝ դիտումների և հավանումների գծային գրաֆիկը (Chart.js)
document.addEventListener('DOMContentLoaded', function () {
    const dataEl = document.getElementById('statsChartData');
    const canvas = document.getElementById('statsLineChart');
    if (!dataEl || !canvas || typeof Chart === 'undefined') return;

    const data = JSON.parse(dataEl.textContent);
    const css = getComputedStyle(document.documentElement);
    const muted = (css.getPropertyValue('--text-faint') || '#99A1AF').trim();
    const grid = (css.getPropertyValue('--border') || '#E5E7EB').trim();

    // Գրադիենտ լցում գծի տակ
    function gradient(ctx, area, rgb) {
        if (!area) return 'rgba(' + rgb + ',0.15)';
        const g = ctx.createLinearGradient(0, area.top, 0, area.bottom);
        g.addColorStop(0, 'rgba(' + rgb + ',0.28)');
        g.addColorStop(1, 'rgba(' + rgb + ',0)');
        return g;
    }

    new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [
                {
                    label: data.viewsLabel,
                    data: data.views,
                    borderColor: '#6369D4',
                    backgroundColor: function (c) { return gradient(c.chart.ctx, c.chart.chartArea, '99,105,212'); },
                    fill: true, tension: 0, borderWidth: 2,
                    pointRadius: 4, pointBackgroundColor: '#fff', pointBorderColor: '#6369D4', pointBorderWidth: 2
                },
                {
                    label: data.likesLabel,
                    data: data.likes,
                    borderColor: '#c084fc',
                    backgroundColor: function (c) { return gradient(c.chart.ctx, c.chart.chartArea, '192,132,252'); },
                    fill: true, tension: 0, borderWidth: 2,
                    pointRadius: 4, pointBackgroundColor: '#fff', pointBorderColor: '#c084fc', pointBorderWidth: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: { legend: { display: false } },
            scales: {
                x: { grid: { display: false }, ticks: { color: muted, font: { size: 12 } } },
                y: {
                    beginAtZero: true,
                    grid: { color: grid },
                    ticks: { color: muted, font: { size: 12 }, precision: 0 }
                }
            }
        }
    });
});