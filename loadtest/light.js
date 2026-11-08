import http from 'k6/http';
import { check, sleep } from 'k6';

// Лёгкая смешанная нагрузка на развёрнутый сайт: чтение страниц и небольшая доля записей.
// Запуск (из корня репозитория):
//   k6 run -e BASE_URL=http://inna-comments-test-1955c.germanywestcentral.cloudapp.azure.com loadtest/light.js
// Параметры: -e VUS=15  -e WRITE_PCT=5 (процент итераций с записью)  -e DURATION=60s
// Капча на сервере должна быть выключена (CAPTCHA_ENABLED=false), иначе записи будут отклоняться.
const BASE_URL = __ENV.BASE_URL;
const VUS = Number(__ENV.VUS || 15);
const WRITE_PCT = Number(__ENV.WRITE_PCT || 5) / 100;
const DURATION = __ENV.DURATION || '60s';

export const options = {
  stages: [
    { duration: '10s', target: VUS },
    { duration: DURATION, target: VUS },
    { duration: '5s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<800'],
    'http_req_duration{name:create}': ['p(95)<1500'],
  },
};

const sortFields = ['date', 'userName', 'email'];
const sortDirs = ['asc', 'desc'];
const pick = (list) => list[Math.floor(Math.random() * list.length)];
const randInt = (min, max) => min + Math.floor(Math.random() * (max - min + 1));

// Контроллер принимает multipart/form-data, поэтому тело собираем вручную.
function multipart(fields) {
  const boundary = `----k6${Math.random().toString(16).slice(2)}`;
  let body = '';
  for (const [name, value] of Object.entries(fields))
    body += `--${boundary}\r\nContent-Disposition: form-data; name="${name}"\r\n\r\n${value}\r\n`;
  body += `--${boundary}--\r\n`;

  return { body, headers: { 'Content-Type': `multipart/form-data; boundary=${boundary}` } };
}

function readPage(name, page, sortBy, sortDir) {
  const res = http.get(`${BASE_URL}/api/comments?page=${page}&sortBy=${sortBy}&sortDir=${sortDir}`, { tags: { name } });
  check(res, { 'status 200': (r) => r.status === 200 });
  return res.status === 200 ? res.json() : null;
}

function create(parentId) {
  const fields = {
    userName: `LT${randInt(1, 999999)}`,
    email: 'loadtest@example.com',
    text: 'Нагрузочный тест <i>light</i>',
  };

  if (parentId)
    fields.parentId = String(parentId);

  const { body, headers } = multipart(fields);
  const res = http.post(`${BASE_URL}/api/comments`, body, { headers, tags: { name: 'create' } });
  check(res, { 'created 201': (r) => r.status === 201 });
}

export default function () {
  if (Math.random() < WRITE_PCT) {
    const page = readPage('page-first', 1, 'date', 'desc');
    const items = page?.items ?? [];
    // Половина записей: ответ на один из корневых комментариев первой страницы.
    create(items.length > 0 && Math.random() < 0.5 ? pick(items).id : null);
  } else if (Math.random() < 0.6) {
    readPage('page-first', 1, 'date', 'desc');
  } else {
    readPage('page-other', randInt(1, 3), pick(sortFields), pick(sortDirs));
  }

  sleep(Math.random() + 0.5);
}
