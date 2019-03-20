"use strict";

var Validation = (function() {
    var errorElement = "div";
    var errorClass = "validation-message";

    function highlight($element) {
        $element.addClass("is-invalid");
    }

    function unhighlight($element) {
        $element.removeClass("is-invalid");
    }

    function placeError($error, $element) {
        clearError($element);

        $error.insertAfter($element);

        $element.data("popper", new Popper($element, $error, {
            placement: "bottom-end",
            removeOnDestroy: true
        }));

        var observer = new MutationObserver(function() {
            var popper = $element.data("popper");
            if (popper) {
                popper.scheduleUpdate();
            }
        });

        observer.observe($error.get(0), {
            childList: true
        });

        $element.data("observer", observer);
    }

    function clearError($element) {
        var observer = $element.data("observer");
        if (observer) {
            observer.disconnect();
            $element.removeData("observer");
        }

        var popper = $element.data("popper");
        if (popper) {
            popper.destroy();
            $element.removeData("popper");
        }
    }

    var validationOptions =  {
        errorElement: errorElement,
        errorClass: errorClass,
        highlight: function(element) {
            highlight($(element));
        },
        unhighlight: function(element) {
            unhighlight($(element));
        },
        errorPlacement: placeError,
        success: function(_$label, element) {
            clearError($(element));
        },
        submitHandler: function(form, event) {
            var $form = $(form);
            var submitHandler = $form.data("submit-handler");
            if (submitHandler) {
                // FIXME: awful hack
                var $clickedElement = $(document.activeElement);
                if ($clickedElement.is("button[type='submit']")) {
                    var action = $clickedElement.prop("formAction");
                    if (action) {
                        $form.prop("action", action);
                    }
                    var method = $clickedElement.prop("formMethod");
                    if (method) {
                        $form.prop("method", method);
                    }
                    return form.submit();
                }

                return submitHandler($form, event);
            } else {
                return form.submit();
            }
        }
    };

    $.validator.setDefaults(validationOptions);
    $.validator.unobtrusive.options = validationOptions;

    return {
        updateErrors: function(errors) {
            $(errorClass).each(function() {
                Validation.clearError($(this));
            });

            Object.keys(errors).forEach(function(name) {
                if (errors[name].length > 0) {
                    $("[name='" + name + "']").each(function() {
                        Validation.setError($(this), errors[name][0]);
                    });
                }
            });
        },
        setError: function($element, message) {
            while ($element.is(":hidden")) {
                $element = $element.parent();
            }
            highlight($element);
            placeError($("<" + errorElement + "/>", { "class": errorClass }).text(message), $element);
        },
        clearError: function($element) {
            while ($element.is(":hidden")) {
                $element = $element.parent();
            }
            unhighlight($element);
            clearError($element);
        }
    };
})();

var Initializers = function() {
    var initializers = [];

    return {
        register: function(f) {
            initializers.push(f);
        },
        run: function() {
            $(function() {
                initializers.forEach(function(f) {
                    f();
                });
            });
        }
    };
}();

Initializers.register(function() {
    if (navigator.appName === "Microsoft Internet Explorer" || !!(navigator.userAgent.match(/Trident/) || navigator.userAgent.match(/rv:11/))) {
        $("#ie-warning").removeClass("d-none");
        $("#content").addClass("d-none");
        return;
    }

    $("#ajax-error-reload").click(function() {
        window.location.reload();
    });

    $(document).ajaxError(function(_event, xhr) {
        if (xhr.status === 0 && xhr.readyState === 0) {
            return;
        }

        var $modal = $("#ajax-error-modal");

        var title = "Unknown error", message = null, status = null;
        if (xhr.status === 0) {
            title = "Connection error";
            message = "Could not connect to server. Make sure you are connected to the internet or try again later.";
        } else {
            status = "Status message: " + xhr.statusText + " (Code: " + xhr.status + ")";
            if (xhr.status >= 500) {
                title = "Server error";
                message = "An internal error occurred on the server or the service is temporarily unavailable.";
            } else if (xhr.status >= 400) {
                if (xhr.status === 401) {
                    title = "Session expired";
                    message = "Your session has expired. Try reloading the page and logging back in.";
                } else if (xhr.status === 409) {
                    title = "Concurrent change";
                    var info = xhr.responseJSON;
                    if (info) {
                        message = "The form was changed by " + info.lastEditedBy + " on " + info.lastEditedOn + ". Try reloading the page.";
                    } else {
                        message = "The form was changed from another location. Try reloading the page.";
                    }
                } else {
                    title = "Client error";
                    message = "Server responded with an error. Try reloading the page.";
                }
            }
        }

        $modal.find("#ajax-error-title").text(title);
        $modal.find("#ajax-error-message").text(message);
        $modal.find("#ajax-error-status").text(status);
        $modal.modal({
            backdrop: "static",
            keyboard: false
        });
    });

    $("[data-validation-message]").each(function() {
        var $element = $(this);
        Validation.setError($element, $element.data("validation-message"));
        $element.removeAttr("data-validation-message");
    });

    $(".list-group").has(".list-group-placeholder").each(function() {
        var $group = $(this);
        $group.data("placeholder", $group.children(".list-group-placeholder").detach());

        var callback = function() {
            if ($group.children(":not(.list-group-placeholder)").length > 0) {
                $group.children(".list-group-placeholder").remove();
            } else if ($group.children(".list-group-placeholder").length === 0) {
                $group.append($group.data("placeholder"));
            }
        };

        var observer = new MutationObserver(callback);
        observer.observe(this, { childList: true });
        callback();
    });

    Validation.updateErrors(JSON.parse($("#validation-errors").html()));
    $('[data-toggle="tooltip"]').tooltip({
        html: true
    });

    $("#content").removeClass("invisible");
});