"use strict";

var Validation = (function() {
    var errorElement = "div";
    var errorClass = "validation-message";
    var previousErrors = {};

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

        $element.parents().add($element).each(function() {
            observer.observe(this, {
                attributes: true
            });
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
            if (submitHandler && event.useHandler) {
                return submitHandler($form, event);
            } else {
                if (event.action) {
                    $form.prop("action", event.action);
                }
                if (event.method) {
                    $form.prop("method", event.method);
                }
                return form.submit();
            }
        }
    };

    $.validator.setDefaults(validationOptions);
    $.validator.unobtrusive.options = validationOptions;

    return {
        updateErrors: function(errors) {
            Object.keys(previousErrors).forEach(function(name) {
                if (!errors[name]) {
                    $("[name='" + name + "']").each(function() {
                        Validation.clearError($(this));
                    });
                }
            });

            Object.keys(errors).forEach(function(name) {
                if (errors[name].length > 0) {
                    $("[name='" + name + "']").each(function() {
                        Validation.setError($(this), errors[name][0]);
                    });
                }
            });

            previousErrors = errors;
        },
        setError: function($element, message) {
            highlight($element);
            placeError($("<" + errorElement + ">").addClass(errorClass).text(message), $element);
        },
        clearError: function($element) {
            unhighlight($element);
            clearError($element);
        }
    };
})();

function showErrorModal(title, message, status) {
    var $modal = $("#error-modal");

    $modal.find("#error-modal-title").text(title);
    $modal.find("#error-modal-message").text(message);
    $modal.find("#error-modal-status").text(status);

    $modal.modal({
        backdrop: "static",
        keyboard: false
    });
}

function initListPlaceholders($root) {
    $(".list-group", $root).has(".list-group-placeholder").each(function() {
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
}

function showToast(title, message, startTime) {
    var $toast = $("#toast");

    var timerId = $toast.data("timer");
    if (timerId) {
        clearInterval(timerId);
    }

    startTime = startTime || moment();
    timerId = setInterval(function() {
        $("#toast-time", $toast).text(startTime.fromNow());
    }, 5000);
    $toast.data("timer", timerId);

    $("#toast-title", $toast).text(title);
    $("#toast-message", $toast).html(message);
    $("#toast-time", $toast).text(startTime.fromNow());
    $toast.toast("show");
}

$(function() {
    if (navigator.appName === "Microsoft Internet Explorer" || !!(navigator.userAgent.match(/Trident/) || navigator.userAgent.match(/rv:11/))) {
        $("#ie-warning").removeClass("d-none");
        $("#content").addClass("d-none");
        return;
    }

    $("#ajax-error-reload").click(function() {
        window.location.replace(window.location.pathname);
    });

    $(document).ajaxError(function(_event, xhr) {
        if (xhr.status === "abort" || xhr.status === 0 && xhr.readyState === 0) {
            return;
        }

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

        showErrorModal(title, message, status);
    });

    initListPlaceholders(document);

    Validation.updateErrors(JSON.parse($("#validation-errors").html()));
    $("[data-toggle='tooltip']").tooltip({
        html: true
    });

    $("#content").removeClass("invisible");
});

(function(document, history, location) {
    var historySupport = !!(history && history.pushState);

    var anchorScrolls = {
        ANCHOR_REGEX: /^#[^ ]+$/,
        OFFSET_HEIGHT_PX: 66,

        init: function() {
            this.scrollToCurrent();
            $(window).on("hashchange", $.proxy(this, "scrollToCurrent"));
            $("body").on("click", "a", $.proxy(this, "delegateAnchors"));
        },

        getFixedOffset: function() {
            return this.OFFSET_HEIGHT_PX;
        },

        scrollIfAnchor: function(href, pushToHistory) {
            var anchorOffset;

            if (!this.ANCHOR_REGEX.test(href)) {
                return false;
            }

            var match = document.getElementById(href.slice(1));

            if (match) {
                anchorOffset = $(match).offset().top - this.getFixedOffset();
                $("html, body").animate({ scrollTop: anchorOffset });

                if (historySupport && pushToHistory) {
                    history.pushState({}, document.title, location.pathname + href);
                }
            }

            return !!match;
        },

        scrollToCurrent: function(e) {
            if (this.scrollIfAnchor(window.location.hash) && e) {
                e.preventDefault();
            }
        },

        delegateAnchors: function(e) {
            var elem = e.target;

            if (this.scrollIfAnchor(elem.getAttribute("href"), true)) {
                e.preventDefault();
            }
        }
    };

    $(document).ready($.proxy(anchorScrolls, "init"));
})(window.document, window.history, window.location);