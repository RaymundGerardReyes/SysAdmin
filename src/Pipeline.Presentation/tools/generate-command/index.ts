function main() {
  const cmdName = process.argv[2];
  if (!cmdName) {
    console.log('Usage: npm run gen:command <CommandName>');
    return;
  }
  console.log(`[gen:command] Generating command '${cmdName}' across contracts, API, fixtures and docs...`);
}

main();
