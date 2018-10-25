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
        $error.insertAfter($element);

        $element.data("popper", new Popper($element, $error, {
            placement: "bottom-end",
            removeOnDestroy: true
        }));

        var observer = new MutationObserver(function() {
            $element.data("popper").scheduleUpdate();
        });

        observer.observe($error.get(0), {
            childList: true
        });

        $element.data("observer", observer);
    }

    function clearError($element) {
        $element.data("observer").disconnect();
        $element.data("popper").destroy();
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
                submitHandler($form, event);
            } else {
                form.submit();
            }
        }
    };

    $.validator.setDefaults(validationOptions);
    $.validator.unobtrusive.options = validationOptions;

    return {
        setError: function($element, message) {
            highlight($element);
            placeError($("<" + errorElement + "/>", { "class": errorClass }).text(message), $element);
        },
        clearError: function($element) {
            unhighlight($element);
            clearError($element);
        }
    };
})();

jQuery(function($) {
    $("#ajax-error-reload").click(function() {
        window.location.reload();
    });

    $(document).ajaxError(function(_event, xhr, _settings, error) {
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
                    message = "The form was changed from another location. Try reloading the page.";
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

    $("#content").removeClass("invisible");
});