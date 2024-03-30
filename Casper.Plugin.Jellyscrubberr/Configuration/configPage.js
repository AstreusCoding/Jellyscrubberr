const pluginId = "6b961c92-5678-45d1-a7ac-dd5003f03460";

let configurationPageElement = $(".jellyscrubberrConfigurationPage");
let configurationFormElement = $(".ConfigurationForm");

const containersBehaviour = {
    "extractionDuringLibraryScan": ["scanBehaviourContainer", true],
    "LocalMediaFolderSaving": ["fileSaveLocationContainer", true],
    "fileSaveLocation": ["customFolderNameContainer", "CustomFolder"]
};

function fetchPluginConfiguration() {
    return ApiClient.getPluginConfiguration(pluginId);
}

function updatePluginConfiguration(config) {
    return ApiClient.updatePluginConfiguration(pluginId, config);
}

function showLoadingMsg() {
    Dashboard.showLoadingMsg();
}

function hideLoadingMsg() {
    Dashboard.hideLoadingMsg();
}

function setInitialConfigurationValues(page, config) {
    for (let key in config) {
        if (config.hasOwnProperty(key)) {
            let element = page.querySelector(`#${key}Value`);
            if (element) {
                // if the element is a checkbox, set the checked attribute
                if (element.type === "checkbox") {
                    element.checked = config[key];
                    handleCheckboxFormChange(key, {checked: config[key]});
                } else {
                    element.value = config[key];
                    handleValueFormChange(key, {value: config[key]});
                }
            } else {
                console.error(`Element with id ${key}Value not found.`);
            }
        }
    }
}

function saveConfiguration(configurationFormElement) {
    showLoadingMsg();

    let configPromise = fetchPluginConfiguration();

    configPromise.then(function (config) {
        for (let key in config) {
            if (config.hasOwnProperty(key)) {
                let element = configurationFormElement.querySelector(`#${key}Value`);
                if (element) {
                    // if the element is a checkbox, get the checked attribute
                    if (element.type === "checkbox") {
                        config[key] = element.checked;
                    } else {
                        config[key] = element.value;
                    }
                } else {
                    console.error(`Element with id ${key}Value not found in the form.`);
                }
            }
        }

        let updatePromise = updatePluginConfiguration(config);
        updatePromise.then(Dashboard.processPluginConfigurationUpdateResult);
    });
}

function setElementVisible(element, show) {
    if (show) {
        element.style.display = "";
    } else {
        element.style.display = "none";
    }
}

function handleCheckboxFormChange(elementName, newValue) {
    let {checked} = newValue;
    let containerBehaviour = containersBehaviour[elementName];

    if (!containerBehaviour) {
      return;
    }

    let containerTargetName = containerBehaviour[0];
    let containerTargetElement = configurationFormElement.querySelector(`#${containerTargetName}`);

    if (!containerTargetElement) {
      return;
    }

    let shouldShow = containerBehaviour[1] === checked;

    setElementVisible(containerTargetElement, shouldShow);
}

function handleValueFormChange(elementName, newValue) {
    let {value} = newValue;
    let containerBehaviour = containersBehaviour[elementName];

    if (!containerBehaviour) {
      return;
    }

    let containerTargetName = containerBehaviour[0];
    let containerTargetElement = configurationFormElement.querySelector(`#${containerTargetName}`);

    if (!containerTargetElement) {
      return;
    }
    
    let shouldShow = containerBehaviour[1] === value;

    setElementVisible(containerTargetElement, shouldShow);
}

function handleValueFormChangeEvent(elementName) {
    let element = configurationFormElement.querySelector(`#${elementName}Value`);
    element.addEventListener("change", function (event) {
        let {value} = this;
        handleValueFormChange(elementName, {value});
    });
}

function handleCheckboxFormChangeEvent(elementName) {
    let element = configurationFormElement.querySelector(`#${elementName}Value`);
    element.addEventListener("change", function (event) {
        let {checked} = this;
        handleCheckboxFormChange(elementName, {checked});
    });
}

function pageLoaded(view, event) {
    // init code here
    showLoadingMsg();

    let page = view;
    configurationPageElement = page.querySelector(".jellyscrubberrConfigurationPage");
    configurationFormElement = page.querySelector(".ConfigurationForm");

    // set up event listeners for the form elements
    for (let key in containersBehaviour) {
        // if the value is a boolean, it's a checkbox
        if (typeof containersBehaviour[key][1] === "boolean") {
            handleCheckboxFormChangeEvent(key);
        } else {
            handleValueFormChangeEvent(key);
        }
    }

    let configPromise = fetchPluginConfiguration();

    // set the initial values when the configuration is fetched
    configPromise.then(function (config) {
        setInitialConfigurationValues(page, config);
        hideLoadingMsg();
    });

    // Save the configuration when the form is submitted
    configurationFormElement.addEventListener("submit", function (event) {
        event.preventDefault();
        saveConfiguration(configurationFormElement);

        return false;
    });
};

export default function (view, params) {
    view.addEventListener('viewshow', function (e) {
        pageLoaded(view, e);
    });
}