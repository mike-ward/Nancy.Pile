var tsc = function(input) {
  var opts = {};
  var host = ts.createCompilerHost(opts);

  var output = '';
  host.writeFile = function (filename, text) { output += text; };
  host.getSourceFile = function() { return ts.createSourceFile('textsource', input, 0, '0'); }
  host.getDefaultLibFilename = function () { return ''; }
  host.getCurrentDirectory = function () { return '/' }
  host.useCaseSensitiveFileNames = function () { return false; }
  host.getCanonicalFileName = function (text) { return text; }
  host.getNewLine = function () { return '\n'; }

  var prog = ts.createProgram(['textsource'], opts, host);
  prog.getDiagnostics();
  var checker = prog.getTypeChecker(true);
  checker.emitFiles();
  return output;
}

var typescriptout = tsc(typescriptin);