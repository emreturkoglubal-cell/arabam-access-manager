/**
 * Giriş sayfasında yüz doğrulama (face-api.js).
 * "Yüz ile doğrula" işaretliyse form gönderilmeden önce kamera ile referans foto karşılaştırılır.
 */
(function () {
    var MODEL_URL = 'https://cdn.jsdelivr.net/gh/justadudewhohacks/face-api.js@0.22.2/weights';
    var MATCH_THRESHOLD = 0.6;
    var POLL_INTERVAL_MS = 500;

    var form = document.querySelector('form[asp-controller="Account"]') || document.querySelector('form[action*="Account/Login"]') || document.querySelector('form');
    if (!form) return;

    var useFaceCheckbox = document.getElementById('useFaceVerification');
    var faceModal = document.getElementById('faceVerifyModal');
    var faceStatus = document.getElementById('faceVerifyStatus');
    var faceRefArea = document.getElementById('faceVerifyRefArea');
    var faceRefImage = document.getElementById('faceRefImage');
    var faceVideo = document.getElementById('faceVerifyVideo');
    var faceCanvas = document.getElementById('faceVerifyCanvas');
    var faceClose = document.getElementById('faceVerifyClose');
    var faceCancel = document.getElementById('faceVerifyCancel');

    var modelsLoaded = false;
    var referenceDescriptor = null;
    var videoStream = null;
    var pollTimer = null;

    function getUsername() {
        var input = form.querySelector('input[name="UserName"]') || form.querySelector('#UserName');
        return input ? (input.value || '').trim() : '';
    }

    function setStatus(msg, isError) {
        if (faceStatus) {
            faceStatus.textContent = msg;
            faceStatus.className = 'text-muted' + (isError ? ' text-danger' : '');
        }
    }

    function stopVideo() {
        if (pollTimer) {
            clearInterval(pollTimer);
            pollTimer = null;
        }
        if (videoStream) {
            videoStream.getTracks().forEach(function (t) { t.stop(); });
            videoStream = null;
        }
        if (faceVideo && faceVideo.srcObject) {
            faceVideo.srcObject = null;
        }
    }

    function closeModal() {
        stopVideo();
        if (faceModal && window.bootstrap) {
            var modal = bootstrap.Modal.getInstance(faceModal);
            if (modal) modal.hide();
        }
    }

    function submitForm() {
        closeModal();
        form.submit();
    }

    function loadModels() {
        if (modelsLoaded) return Promise.resolve();
        return Promise.all([
            faceapi.nets.ssdMobilenetv1.loadFromUri(MODEL_URL),
            faceapi.nets.faceLandmark68Net.loadFromUri(MODEL_URL),
            faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL)
        ]).then(function () {
            modelsLoaded = true;
        });
    }

    function getReferenceDescriptor(img) {
        return faceapi.detectSingleFace(img)
            .withFaceLandmarks()
            .withFaceDescriptor();
    }

    function getVideoDescriptor() {
        return faceapi.detectSingleFace(faceVideo)
            .withFaceLandmarks()
            .withFaceDescriptor();
    }

    function startFaceVerification() {
        var username = getUsername();
        if (!username) {
            setStatus('Önce kullanıcı adını girin.', true);
            submitForm();
            return;
        }

        var base = (form.action || '').replace(/\/Login(\?.*)?$/i, '');
        var photoUrl = base + '/PersonnelPhoto?username=' + encodeURIComponent(username);
        faceRefImage.onerror = function () {
            setStatus('Bu kullanıcı için kayıtlı fotoğraf yok. Parola ile giriş yapılıyor.', true);
            setTimeout(submitForm, 1500);
        };
        faceRefImage.onload = function () {
            faceRefArea && faceRefArea.classList.remove('d-none');
            setStatus('Modeller yükleniyor...');
            loadModels().then(function () {
                setStatus('Referans fotoğraf analiz ediliyor...');
                getReferenceDescriptor(faceRefImage).then(function (refDesc) {
                    if (!refDesc) {
                        setStatus('Referans fotoğrafta yüz bulunamadı. Parola ile giriş yapılıyor.', true);
                        setTimeout(submitForm, 2000);
                        return;
                    }
                    referenceDescriptor = refDesc;
                    setStatus('Kamera açılıyor...');
                    startVideoAndCompare();
                }).catch(function () {
                    setStatus('Referans analiz hatası. Parola ile giriş yapılıyor.', true);
                    setTimeout(submitForm, 2000);
                });
            }).catch(function () {
                setStatus('Model yükleme hatası. Parola ile giriş yapılıyor.', true);
                setTimeout(submitForm, 2000);
            });
        };
        faceRefImage.src = photoUrl;
    }

    function startVideoAndCompare() {
        navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user', width: 640, height: 480 } })
            .then(function (stream) {
                videoStream = stream;
                faceVideo.srcObject = stream;
                setStatus('Yüzünüzü kameraya getirin...');
                pollForMatch();
            })
            .catch(function () {
                setStatus('Kamera erişilemedi. Parola ile giriş yapılıyor.', true);
                setTimeout(submitForm, 2500);
            });
    }

    function pollForMatch() {
        pollTimer = setInterval(function () {
            if (!referenceDescriptor || !faceVideo.videoWidth) return;
            getVideoDescriptor().then(function (queryDesc) {
                if (!queryDesc) return;
                var matcher = new faceapi.FaceMatcher([
                    new faceapi.LabeledFaceDescriptors('ref', [referenceDescriptor.descriptor])
                ]);
                var match = matcher.findBestMatch(queryDesc.descriptor);
                if (match && match.label !== 'unknown' && match.distance <= MATCH_THRESHOLD) {
                    setStatus('Yüz doğrulandı. Giriş yapılıyor...');
                    clearInterval(pollTimer);
                    pollTimer = null;
                    setTimeout(submitForm, 400);
                }
            }).catch(function () {});
        }, POLL_INTERVAL_MS);
    }

    form.addEventListener('submit', function (e) {
        if (!useFaceCheckbox || !useFaceCheckbox.checked) return;

        e.preventDefault();

        var username = getUsername();
        if (!username) {
            form.submit();
            return;
        }

        setStatus('Hazırlanıyor...');
        faceRefArea && faceRefArea.classList.add('d-none');
        faceRefImage.onload = null;
        faceRefImage.onerror = null;
        referenceDescriptor = null;

        if (faceModal && window.bootstrap) {
            var modal = new bootstrap.Modal(faceModal);
            modal.show();
            faceModal.addEventListener('hidden.bs.modal', function onHidden() {
                faceModal.removeEventListener('hidden.bs.modal', onHidden);
                stopVideo();
            }, { once: true });
        }

        startFaceVerification();
    });

    if (faceClose) faceClose.addEventListener('click', closeModal);
    if (faceCancel) faceCancel.addEventListener('click', closeModal);
})();
