import http from 'k6/http';
import { check } from 'k6';

// Нагрузка на чтение. Запуск (из корня репозитория):
//   k6 run --insecure-skip-tls-verify loadtest/read.js
// Другие параметры: -e BASE_URL=https://localhost:7100 -e VUS=50 -e MAX_ID=1000000 -e TOTAL_PAGES=4000
//   -e DEEP=10  (процент запросов на последние и средние страницы, остальное: первая, первые пять и by-ids)
const BASE_URL = __ENV.BASE_URL || 'https://localhost:7100';
const VUS = Number(__ENV.VUS || 50);
const MAX_ID = Number(__ENV.MAX_ID || 1000000);
const TOTAL_PAGES = Number(__ENV.TOTAL_PAGES || 4000);
const DEEP = Number(__ENV.DEEP || 10) / 100;

// Доли трафика: первая страница 50%, by-ids 5%, глубокие страницы DEEP (40% последние, 60% средние), остальное первые пять.
const FIRST_SHARE = 0.5;
const TAIL_SHARE = DEEP * 0.4;
const MIDDLE_SHARE = DEEP * 0.6;
const HEAD_SHARE = 1 - FIRST_SHARE - 0.05 - DEEP;

export const options = {
  stages: [
    { duration: '10s', target: VUS },
    { duration: '40s', target: VUS },
    { duration: '5s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
    'http_req_duration{name:page-first}': ['p(95)<300'],
    'http_req_duration{name:page-head}': ['p(95)<300'],
    'http_req_duration{name:page-tail}': ['p(95)<1000'],
    'http_req_duration{name:page-middle}': ['p(95)<1000'],
    'http_req_duration{name:by-ids}': ['p(95)<300'],
  },
};

const sortFields = ['date', 'userName', 'email'];
const sortDirs = ['asc', 'desc'];

const pick = (list) => list[Math.floor(Math.random() * list.length)];
const randInt = (min, max) => min + Math.floor(Math.random() * (max - min + 1));

function getPage(name, page, sortBy, sortDir) {
  const res = http.get(`${BASE_URL}/api/comments?page=${page}&sortBy=${sortBy}&sortDir=${sortDir}`, { tags: { name } });
  check(res, { 'status 200': (r) => r.status === 200 });
}

export default function () {
  const roll = Math.random();

  if (roll < FIRST_SHARE) {
    // Первая страница по умолчанию: самый частый запрос.
    getPage('page-first', 1, 'date', 'desc');
  } else if (roll < FIRST_SHARE + HEAD_SHARE) {
    // Первые 5 страниц с любой сортировкой.
    getPage('page-head', randInt(1, 5), pick(sortFields), pick(sortDirs));
  } else if (roll < FIRST_SHARE + HEAD_SHARE + TAIL_SHARE) {
    getPage('page-tail', randInt(TOTAL_PAGES - 4, TOTAL_PAGES), pick(sortFields), pick(sortDirs));
  } else if (roll < FIRST_SHARE + HEAD_SHARE + TAIL_SHARE + MIDDLE_SHARE) {
    // Страницы из середины: кэш на них не работает, нагрузка идёт прямо в базу.
    getPage('page-middle', randInt(6, TOTAL_PAGES - 5), pick(sortFields), pick(sortDirs));
  } else {
    // Подгрузка 10 последних комментариев по Id, как после сигнала о новых.
    const ids = Array.from({ length: 10 }, () => `ids=${MAX_ID - randInt(0, 200)}`).join('&');
    const res = http.get(`${BASE_URL}/api/comments/by-ids?${ids}`, { tags: { name: 'by-ids' } });
    check(res, { 'status 200': (r) => r.status === 200 });
  }
}
