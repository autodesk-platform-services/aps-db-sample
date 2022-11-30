﻿import { initViewer, loadModel } from './viewer.js';
import { initHubsTree, initOssTree } from './sidebar.js';

const login = document.getElementById('login');
try {
  const viewer = await initViewer(document.getElementById('preview'));
  initOssTree('#osstree', (id) => loadModel(viewer, id));
  const resp = await fetch('/api/auth/profile');
  if (resp.ok) {
    const user = await resp.json();
    login.innerText = `Logout (${user.name})`;
    login.onclick = () => {
      // Log the user out (see https://aps.autodesk.com/blog/log-out-forge)
      const iframe = document.createElement('iframe');
      iframe.style.visibility = 'hidden';
      iframe.src = 'https://accounts.autodesk.com/Authentication/LogOut';
      document.body.appendChild(iframe);
      iframe.onload = () => {
        window.location.replace('/api/auth/logout');
        document.body.removeChild(iframe);
      };
    }
    /*const viewer = await initViewer(document.getElementById('preview'));*/
    initHubsTree('#hubstree', (id) => loadModel(viewer, window.btoa(id).replace(/=/g, '')));
  } else {
    login.innerText = 'Login';
    login.onclick = () => window.location.replace('/api/auth/login');
  }
  login.style.visibility = 'visible';
} catch (err) {
  alert('Could not initialize the application. See console for more details.');
  console.error(err);
}