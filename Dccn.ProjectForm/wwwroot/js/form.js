"use strict";

jQuery(function($) {
    // Misc
    function generateUniqueIndex() {
        var uuid;

        do {
            uuid = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function(c) {
                var r = Math.random() * 16 | 0;
                var v = c === "x" ? r : r & 0x3 | 0x8;
                return v.toString(16);
            });
        } while ($("[data-index='" + uuid + "']").length > 0);

        return uuid;
    }

    function registerSubmitHandlers(root) {
        $(root).find("[data-submit-on]").each(function() {
            var $input = $(this);
            var $form = $("#form");
            var sectionId = $input.parents(".form-section").prop("id");

            $input.on($input.data("submit-on"), function() {
                $form.data("section-id", sectionId);
                $form.submit();
            });
        });
    }

    function initializeUserList(userType, itemDefaults, createItem) {
        var $autocomplete = $(".aa-input.user-query[data-usertype='" + userType + "']");
        var $userlist = $(".user-items[data-usertype='" + userType + "']");

        $(".user-add[data-usertype='" + userType + "']").click(function(event) {
            var id = $autocomplete.data("id");
            if (!id || $userlist.find("[data-id='" + id + "']").length > 0) {
                $autocomplete.addClass("border-danger");
                event.stopImmediatePropagation();
                return;
            }

            $userlist.append(createItem(Object.assign({
                index: id,
                id: id,
                name: $autocomplete.autocomplete("val")
            }, itemDefaults)));

            $autocomplete.autocomplete("val", "");
            $autocomplete.trigger("input");
        });

        $userlist.children(":not(.list-group-placeholder)").replaceWith(function() {
            return createItem($(this).data());
        });
    }

    $(window).on("beforeunload", function() {
        var errorCount = $(".is-invalid").length;
        if (errorCount > 0) {
            return "The form contains input validation errors. Incorrect fields are not saved. Are you sure you want to leave the page?";
        }
        return undefined;
    });

    $("#form").data("submit-handler", function($form) {
        function updateErrors(errors) {
            $(".is-invalid").each(function() {
                Validation.clearError($(this));
            });

            Object.keys(errors).forEach(function(name) {
                var $inputs = $("[name='" + name + "']");
                if (errors[name].length > 0) {
                    $inputs.each(function() {
                        Validation.setError($(this), errors[name][0]);
                    });
                }
            });
        }

        var sectionId = $form.data("section-id");
        var $items, url;
        if (sectionId) {
            url = $form.attr("action").replace("__SECTION__", encodeURIComponent(sectionId));
            $items = $form.find(":input").filter(function() {
                var parentSectionId = $(this).parents(".form-section").prop("id");
                return !parentSectionId || parentSectionId === sectionId;
            });
        } else {
            url = $form.attr("action").replace("__SECTION__", "");
            $items = $form;
        }

        if ($form.data("recently-submitted")) {
            $form.data("resubmit", true);
            return;
        }
        $form.data("recently-submitted", true);
        $.ajax({
            type: $form.attr("method"),
            url: url,
            data: $items.serialize(),
            dataType: "json"
        }).done(function(result) {
            $(".timestamp").val(result.timestamp);
            updateErrors(result.errors);
        }).always(function() {
            setTimeout(function() {
                $form.data("recently-submitted", false);
                if ($form.data("resubmit")) {
                    $form.data("resubmit", false);
                    $form.submit();
                }
            }, 1000);
        });
    });

    $("#request-approval-modal").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        var section = $button.data("section");

        var $form = $(this).find("form");
        $form.prop("action", $form.data("url").replace("__SECTION__", encodeURIComponent(section)));
    });

    $(".radio-panel-input").change(function() {
        var $input = $(this);
        if ($input.prop("checked")) {
            $($input.data("target")).collapse("show");
        }
    });

    $(".radio-panel-input").each(function() {
        var $input = $(this);
        var $collapse = $($input.data("target"));
        $collapse.on("hide.bs.collapse", function() {
            $collapse.prop("disabled", true);
        });
        $collapse.on("show.bs.collapse", function() {
            $collapse.prop("disabled", false);
        });

        if ($input.prop("checked")) {
            $collapse.addClass("show");
        } else {
            $collapse.prop("disabled", true);
        }
    });



    // Ethics
    $(".ethics-approval").change(function() {
        var $option = $(this.selectedOptions[0]);
        var description = $option.data("description");
        if (description) {
            $(".ethics-custom").val(description).prop("disabled", true);
        } else {
            $(".ethics-custom").val("").prop("disabled", false);
        }
    }).change();



    // Experiment
    var labItemTemplate = $.templates("#lab-item-template");

    function createLabItem(data) {
        var $item = $(labItemTemplate.render(data));

        function updateTotalDuration() {
            var subjects = parseInt($item.find(".lab-item-subjects").val());
            var sessions = parseInt($item.find(".lab-item-sessions").val());
            var duration = parseInt($item.find(".lab-item-duration").val());
            var totalDuration = moment.duration(subjects * sessions * duration, "minutes");
            $item.find(".lab-item-total-duration").text(totalDuration.isValid() ? totalDuration.asHours().toFixed() + " hour(s)" : "");
        }

        $item.find(".remove-lab-item").click(function() {
            $item.remove();
            updateLabItems();
            updateStandardQuota();
        });

        $item.find(".lab-item-subjects,.lab-item-sessions,.lab-item-duration").change(updateTotalDuration);
        $item.find(".lab-item-subjects,.lab-item-sessions").change(updateStandardQuota);

        updateTotalDuration();
        updateStandardQuota();
        registerSubmitHandlers($item);

        return $item;
    }

    function updateLabItems() {
        var $container = $(".lab-items");
        if ($container.children(".lab-item-expanded").length === 0) {
            $("#quota-standard").prop("disabled", true);
            $("#quota-custom").prop("checked", true).change();
        } else {
            $("#quota-standard").prop("disabled", false);
        }
    }

    function updateStandardQuota() {
        var $items = $(".lab-item-expanded");
        if ($items.length > 0) {
            var quota = 0;
            $items.each(function() {
                var $item = $(this);

                var subjects = parseInt($item.find(".lab-item-subjects").val()) || 0;
                var sessions = parseInt($item.find(".lab-item-sessions").val()) || 0;
                var sessionStorage = $item.data("storage-session");
                var fixedStorage = $item.data("storage-fixed");

                quota += subjects * sessions * sessionStorage + fixedStorage;
            });
            $(".quota-standard-value").text(quota.toFixed(1) + " GB");
        } else {
            $(".quota-standard-value").first().text("requires at least one lab");
        }
    }

    $(".add-lab-item").click(function() {
        var $button = $(this);
        $(".lab-items").append(createLabItem({
            index: generateUniqueIndex(),
            modality: $button.data("modality"),
            storageFixed: $button.data("storage-fixed"),
            storageSession: $button.data("storage-session")
        }));
        updateLabItems();
        updateStandardQuota();
    });

    $(".lab-item").replaceWith(function() {
        return createLabItem($(this).data());
    });
    updateLabItems();
    updateStandardQuota();



    $(".user-query").each(function() {
        var $autocomplete = $(this);

        $autocomplete.on("keypress", function(event) {
            if (event.key === "Enter") {
                $(".user-add[data-usertype='" + $autocomplete.data("usertype") + "']").click();
                event.preventDefault();
            }
        });

        $autocomplete.on("input", function() {
            $autocomplete.removeClass("border-success").removeClass("border-danger");
            $autocomplete.removeData("id");
        });

        $autocomplete.on("autocomplete:selected", function(_event, suggestion) {
            $autocomplete.addClass("border-success");
            $autocomplete.data("id", suggestion.id);
        });

        $autocomplete.autocomplete({
                minLength: 2,
                highlight: true,
                autoselect: true
            },
            {
                source: function(query, callback) {
                    $.getJSON({
                        url: $autocomplete.data("url").replace("__QUERY__", encodeURIComponent(query))
                    }).done(function(data) {
                        callback(data);
                    });
                },
                display: function(suggestion) {
                    return $.fn.autocomplete.escapeHighlightedString(suggestion.name);
                },
                templates: {
                    suggestion: function(suggestion) {
                        return $("<div></div>")
                            .text(suggestion.name + " ")
                            .append($("<span></span>", { "class": "badge badge-info" }).text(suggestion.id))
                            .html();
                    },
                    empty: function(params) {
                        return $("<div></div>")
                            .text(" No results for '" + params.query + "'.")
                            .html();
                    }
                },
                debounce: 100
            }
        );
    });

    var experimenterItemTemplate = $.templates("#experimenter-item-template");
    initializeUserList("experimenter", {}, function(data) {
        var $item = $(experimenterItemTemplate.render(data));
        $item.find(".remove-experimenter").click(function() {
            $(this).parents(".experimenter-item-expanded").remove();
        });
        return $item;
    });



    // Data management
    var accessItemTemplate = $.templates("#access-item-template");
    initializeUserList("access", { role: "Viewer", canEdit: true, canRemove: true }, function(data) {
        var $item = $(accessItemTemplate.render(data));
            $item.find(".remove-access").click(function() {
            $(this).parents(".access-item-expanded").remove();
        });
        return $item;
    });



    $(".repository-user-warning").each(function() {
        var $alert = $(this);
        $.getJSON({
            url: $alert.data("url")
        }).done(function(json) {
            if (json === false) {
                $alert.removeClass("d-none");
            }
        });
    });



    // Payment
    $(".cost-subjects,.cost-average").change(function() {
        var subjects = parseInt($(".cost-subjects").val());
        var averageCost = parseFloat($(".cost-average").val());
        var totalCost = subjects * averageCost;
        if (!isNaN(totalCost)) {
            $(".cost-predicted").val(totalCost.toFixed(2));
        } else {
            $(".cost-predicted").val("");
        }
    });

    $(".quota-overrule").change(function() {
        var $this = $(this);
        if ($this.prop("checked")) {
            $(".quota-custom").prop("disabled", false);
        } else {
            $(".quota-custom").prop("disabled", true).val("");
        }
    });



    // Init
    registerSubmitHandlers(document);
});