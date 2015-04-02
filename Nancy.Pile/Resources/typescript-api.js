
var tsc = function (input) {
  var opts = {};
  var host = ts.createCompilerHost(opts);

  var output = '';
  host.writeFile = function (filename, text) { output += text; };
  host.getDefaultLibFilename = function () { return 'lib'; }
  host.getCurrentDirectory = function () { return ''; }
  host.useCaseSensitiveFileNames = function () { return false; }
  host.getCanonicalFileName = function (text) { return text; }
  host.getNewLine = function () { return '\n'; }

  host.getSourceFile = function (fn) {
    return ts.createSourceFile(fn, fn === 'input.ts' ? input : '', opts.target, '0');
  }

  var prog = ts.createProgram(['input'], opts, host);
  prog.getDiagnostics();
  var checker = prog.getTypeChecker(true);
  checker.emitFiles();
  return output;
}

var result = tsc(source);