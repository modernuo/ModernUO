const fs = require('fs').promises;

const convertJSON = (json) => {
  if (typeof(json) !== 'object') return json;

  if (Array.isArray(json)) return json.map(v => convertJSON(v));

  return Object.entries(json).reduce((acc, entry) => ({
    ...acc,
    [`${entry[0].charAt(0).toLowerCase()}${entry[0].substr(1)}`]: convertJSON(entry[1]),
  }), {});
}

const run = async () => {
  const files = (await fs.readdir('./'))
    .filter(file => file.endsWith('.json'));

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    const json = JSON.parse(await fs.readFile(file));
    const newJSON = convertJSON(json);
    await fs.writeFile(`./output/${file}`, JSON.stringify(newJSON, null, '  '));
  }

  console.log('complete');
};

run();