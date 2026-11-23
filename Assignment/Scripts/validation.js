// Client-side rules for the custom validation attributes in Validation/DateAfterAttribute.cs.
(function ($) {
    'use strict';
    if (!$ || !$.validator) return;

    var parse = function (v) { return v ? new Date(v) : null; };

    // dateafter: this value must be later than another field's value.
    $.validator.addMethod('dateafter', function (value, element, otherSelector) {
        if (!value) return true;
        var other = $(element).closest('form').find(otherSelector).val();
        var a = parse(value), b = parse(other);
        return !a || !b || a > b;
    });
    $.validator.unobtrusive.adapters.add('dateafter', ['other'], function (options) {
        var name = options.params.other.replace('*.', '');
        options.rules.dateafter = '[name="' + name + '"]';
        options.messages.dateafter = options.message;
    });

    // notinpast: the date cannot be earlier than now (5 minutes' grace, as on the server).
    $.validator.addMethod('notinpast', function (value) {
        var d = parse(value);
        return !d || d.getTime() >= Date.now() - 5 * 60 * 1000;
    });
    $.validator.unobtrusive.adapters.addBool('notinpast');
})(window.jQuery);
