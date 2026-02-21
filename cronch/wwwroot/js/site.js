// Find and show any toasts
document.querySelectorAll('.toast').forEach(toastEl => new bootstrap.Toast(toastEl).show());

function initializeScriptEditors(rootEl) {
    (rootEl || document).querySelectorAll('pre[data-script-editor].script-editor-loading').forEach(function (editorEl) {
        var hiddenEl = document.getElementById(editorEl.dataset.scriptEditor);
        if (hiddenEl) {
            configureCronchScriptEditor(editorEl, hiddenEl);
        }
    });
}
window.addEventListener('load', function () { initializeScriptEditors(); });
document.addEventListener('htmx:afterSettle', function (evt) { initializeScriptEditors(evt.detail.elt); });

function convertHighlightLanguageToAce(lang) {
    var aceLang = null;
    switch (lang) {
        case 'c':
        case 'cpp':
            aceLang = 'c_cpp';
            break;
        case 'csharp':
            aceLang = 'csharp';
            break;
        case 'go':
            aceLang = 'golang';
            break;
        case 'java':
            aceLang = 'java';
            break;
        case 'javascript':
            aceLang = 'javascript';
            break;
        case 'json':
            aceLang = 'json';
            break;
        case 'kotlin':
            aceLang = 'kotlin';
            break;
        case 'lua':
            aceLang = 'lua';
            break;
        case 'perl':
            aceLang = 'perl';
            break;
        case 'php-template':
        case 'php':
            aceLang = 'php';
            break;
        case 'python-repl':
        case 'python':
            aceLang = 'python';
            break;
        case 'ruby':
            aceLang = 'ruby';
            break;
        case 'typescript':
            aceLang = 'typescript';
            break;
        case 'xml':
            aceLang = 'xml';
            break;
        case 'yaml':
            aceLang = 'yaml';
            break;
        default:
            aceLang = 'sh';
            break;
    }
    return aceLang;
}

function getAceLanguage(scriptContents) {
    var regex = /\[\[\[CRONCH:syntax:(.+)\]\]\]/g;
    var match = regex.exec(scriptContents);
    var syntax = match ? match[1] : null;
    return syntax || convertHighlightLanguageToAce(hljs.highlightAuto(scriptContents).language);
}

function configureCronchScriptEditor(editorElement, hiddenValueElement) {
    var scriptUpdateTimer = null;
    var determineLanguageTimer = null;
    var selectedAceLanguage = 'sh';
    try {
        selectedAceLanguage = getAceLanguage(hiddenValueElement.value);
    } catch (e) {
        console.warn('Error occurred while detecting language', e);
    }
    editorElement.setAttribute('detected-language', selectedAceLanguage);
    var editor = ace.edit(editorElement, {
        autoScrollEditorIntoView: true,
        useWorker: false,
        cursorStyle: 'slim',
        useSoftTabs: true,
        tabSize: 2,
        navigateWithinSoftTabs: true,
    });
    editor.setTheme('ace/theme/tomorrow');
    try {
        editor.session.setMode(`ace/mode/${selectedAceLanguage}`);
    } catch (e) {
        console.warn('Error occurred while setting language', e);
    }
    editor.session.on('change', function () {
        clearTimeout(scriptUpdateTimer);
        scriptUpdateTimer = setTimeout(() => {
            hiddenValueElement.value = editor.session.getValue();
        }, 10);
        clearTimeout(determineLanguageTimer);
        determineLanguageTimer = setTimeout(() => {
            var aceLang = 'sh';
            try {
                aceLang = getAceLanguage(editor.session.getValue());
            } catch (e) {
                console.warn('Error occurred while detecting language', e);
            }
            if (aceLang !== selectedAceLanguage) {
                selectedAceLanguage = aceLang;
                try {
                    editor.session.setMode(`ace/mode/${selectedAceLanguage}`);
                    editorElement.setAttribute('detected-language', selectedAceLanguage);
                } catch (e) {
                    console.warn('Error occurred while setting language', e);
                }
            }
        }, 200);
    });
    editorElement.classList.remove('script-editor-loading');
}
