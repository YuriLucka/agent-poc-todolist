const { chromium } = require(require('child_process').execSync('npm root -g').toString().trim() + '/playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 420, height: 800 } });
  const errs = [];
  p.on('console', m => m.type() === 'error' && errs.push(m.text()));
  await p.goto('http://localhost:5080');
  await p.waitForSelector('#form');
  for (const [t, d, pr] of [['Comprar leite', 'Integral, 2 caixas', 'Alta'], ['Estudar Blazor', '', 'Media']]) {
    await p.fill('#title', t); await p.fill('#description', d);
    await p.selectOption('#priority', pr); await p.fill('#dueDate', '2030-01-15');
    await p.click('button[type=submit]');
  }
  await p.check('li:nth-child(2) input[type=checkbox]');
  await p.screenshot({ path: '.agent/screenshots/data.png' });
  console.log('errors', errs);
  await b.close();
})();
