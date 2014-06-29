angular.module('app.controllers')
  .controller('myController', ['mystring', function (mystring) {
    this.message = mystring;
  }
]);