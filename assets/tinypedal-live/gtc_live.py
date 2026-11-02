# GTC Companion integration for TinyPedal 2.52.0.
# Copyright (C) 2026 GTC Companion contributors.
# SPDX-License-Identifier: GPL-3.0-only
"""Apply Companion preset updates in TinyPedal's owning UI thread."""
import json
import logging
import os
import sys
import tempfile
from PySide2.QtCore import QObject, QTimer
import tinypedal
from .. import cfg, app_signal

log = logging.getLogger(__name__)


def norm(path):
    return os.path.normcase(os.path.realpath(path))


class LiveSettings(QObject):
    def __init__(self, parent):
        super().__init__(parent)
        base = os.path.dirname(os.path.dirname(os.path.dirname(sys.executable)))
        self.root = os.environ.get('GTC_COMPANION_DATA_DIR') or os.path.join(base, 'data')
        self.pending = os.path.join(self.root, 'engine-pending-tiny.json')
        self.ack = os.path.join(self.root, 'engine-live-tiny-ack.json')
        self.timer = QTimer(self)
        self.timer.timeout.connect(self.tick)
        self.timer.start(1000)

    def tick(self):
        if cfg.is_saving or os.path.exists(self.ack) or not os.path.isfile(self.pending):
            return
        try:
            with open(self.pending, encoding='utf-8') as stream:
                pending = json.load(stream)
            if pending.get('Error') or pending.get('Engine') != 'tiny':
                return
            current = norm(os.path.join(cfg.path.settings, cfg.filename.setting))
            changes = [item for item in pending.get('Changes', [])
                       if not item.get('Xml') and norm(item.get('File', '')) == current]
            if not changes:
                return
            if cfg.filename.setting in cfg.user.filelock:
                raise ValueError('O perfil do TinyPedal está bloqueado.')
            replacements = []
            for change in changes:
                keys = change.get('Keys')
                if not isinstance(keys, list) or len(keys) != 2:
                    raise ValueError('Definição do TinyPedal inválida.')
                group = cfg.user.setting.get(keys[0])
                if not isinstance(group, dict) or keys[1] not in group:
                    raise ValueError('A definição já não existe no perfil ativo.')
                old = group[keys[1]]
                if old != change.get('Before') and old != change.get('Value'):
                    raise ValueError('A definição foi alterada no TinyPedal. Recarrega antes de guardar.')
                replacements.append((group, keys[1], old, change.get('Value')))
            for group, key, old, value in replacements:
                group[key] = value
            try:
                cfg.save(0)
                tinypedal.reload(False)
                with open(current, encoding='utf-8-sig') as stream:
                    saved = json.load(stream)
                if any(saved.get(c['Keys'][0], {}).get(c['Keys'][1]) != c['Value'] for c in changes):
                    raise IOError('O TinyPedal não confirmou a gravação do perfil.')
                app_signal.refresh.emit(True)
                self.respond(changes, None)
            except Exception:
                for group, key, old, value in replacements:
                    if group.get(key) == value:
                        group[key] = old
                raise
        except Exception as error:
            log.exception('GTC Companion live settings failed')
            try:
                self.respond([], str(error))
            except Exception:
                log.exception('GTC Companion live response failed')

    def respond(self, changes, error):
        os.makedirs(self.root, exist_ok=True)
        fd, temp = tempfile.mkstemp(prefix='tiny-live-', suffix='.tmp', dir=self.root)
        try:
            with os.fdopen(fd, 'w', encoding='utf-8') as stream:
                json.dump({'Changes': changes, 'Error': error}, stream)
            os.replace(temp, self.ack)
        finally:
            if os.path.exists(temp):
                os.remove(temp)
