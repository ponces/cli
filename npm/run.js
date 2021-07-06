#! /usr/bin/env node
"use strict";
const { spawn } = require('child_process');
const envPaths = require('env-paths');
const path = require('path');
const fs = require('fs');
const isInstalledGlobally = require('is-installed-globally');
const node_modules = require('node_modules-path');
const dbg = require('debug');
const { parsePackageJson, ARCH_MAPPING, PLATFORM_MAPPING } = require('./utils');

const opts = parsePackageJson(__dirname);
const debug = dbg("cmf:debug");

debug("Executing cmf-cli");
let exePath = null;
debug("Determining if cli is installed globally or locally...");
if (isInstalledGlobally) {
  debug("cli is installed globally. Getting binary location from user profile...");
  const paths = envPaths("cmf-cli", {suffix: ""});
  exePath = path.join(paths.data, opts.binName);
} else {
  debug("cli is installed locally. Getting binary from node_modules/.bin/cmf-cli...");
  exePath = path.join(node_modules(), ".bin", "cmf-cli", opts.binName);
}
debug("Obtained binary path: " + exePath);

debug(`Spawning cmf-cli from ${exePath} with args ${process.argv.slice(2)} and piping.`);
const child = spawn(exePath, process.argv.slice(2), {stdio: "inherit"});
child.on('close', (code) => {
  debug("Process exited with code " + code);
  process.exitCode = code;
});