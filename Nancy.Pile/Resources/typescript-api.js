
var tsc = function (input, lib) {
  var opts = {};
  var host = ts.createCompilerHost(opts);

  var output = '';
  host.writeFile = function (filename, text) { output += text; };
  host.getDefaultLibFilename = function () { return 'lib.d.ts'; }
  host.getCurrentDirectory = function () { return ''; }
  host.useCaseSensitiveFileNames = function () { return false; }
  host.getCanonicalFileName = function (fn) { return fn; }
  host.getNewLine = function () { return '\n'; }

  host.getSourceFile = function (fn) {
    if (fn === 'input.ts') return ts.createSourceFile(fn, input, opts.target, '0');
    if (fn === 'lib.d.ts') return ts.createSourceFile(fn, lib, opts.target, '0');
    return undefined;
  }

  var prog = ts.createProgram(['input'], opts, host);
  var errors = prog.getDiagnostics();
  if (!errors.length) {
    var checker = prog.getTypeChecker(true);
    checker.getDiagnostics();
    checker.emitFiles();
  }
  return (errors.length) 
    ? errors.map(function (e) { return e.file.filename + "(" + e.file.getLineAndCharacterFromPosition(e.start).line + "): " + e.messageText; })
    : output;
}

var result = tsc(source, libSource);