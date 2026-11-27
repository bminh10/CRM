import { useEffect, useState, type FormEvent } from 'react'
import { AlertTriangle, KeyRound, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { changePassword, getUserProfile, setupOtp, updateUserProfile, verifyOtpSetup, type OtpSetup, type UserProfile } from '@/services/auth'

function ProfilePage({ onProfileUpdated }: { onProfileUpdated: (fullName: string) => void }) {
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [setup, setSetup] = useState<OtpSetup | null>(null)
  const [email, setEmail] = useState('')
  const [setupEmail, setSetupEmail] = useState('')
  const [setupCode, setSetupCode] = useState('')
  const [changeCode, setChangeCode] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  async function submitProfileUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!profile || saving) return
    if (!profile.fullName.trim()) { setError('Họ tên không được để trống.'); return }
    setSaving(true); setError(''); setMessage('')
    try {
      const updated = await updateUserProfile(profile.fullName.trim(), profile.avatarUrl ?? '')
      setProfile(current => current ? { ...current, fullName: updated.fullName, avatarUrl: updated.avatarUrl } : current)
      onProfileUpdated(updated.fullName)
      setMessage('Đã cập nhật thông tin cá nhân.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể cập nhật thông tin cá nhân.')
    } finally { setSaving(false) }
  }

  useEffect(() => {
    let active = true
    getUserProfile()
      .then(data => { if (active) setProfile(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải hồ sơ.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  async function beginSetup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    setSaving(true); setError(''); setMessage('')
    try {
      setSetup(await setupOtp(setupEmail.trim()))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể bắt đầu thiết lập OTP.')
    } finally { setSaving(false) }
  }

  async function confirmSetup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving || !setup) return
    setSaving(true); setError(''); setMessage('')
    try {
      await verifyOtpSetup(setupEmail.trim(), setupCode)
      setProfile(current => current ? { ...current, isOtpConfigured: true } : current)
      setSetup(null); setSetupCode('')
      setMessage('Đã bật xác thực OTP cho tài khoản.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể xác nhận mã OTP.')
    } finally { setSaving(false) }
  }

  async function submitPasswordChange(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    if (newPassword.length < 12) { setError('Mật khẩu mới cần ít nhất 12 ký tự.'); return }
    if (newPassword !== confirmPassword) { setError('Mật khẩu xác nhận không khớp.'); return }
    setSaving(true); setError(''); setMessage('')
    try {
      const result = await changePassword(email.trim(), changeCode, newPassword)
      setMessage(result.message)
      setEmail(''); setChangeCode(''); setNewPassword(''); setConfirmPassword('')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể đổi mật khẩu.')
    } finally { setSaving(false) }
  }

  return <section className="mx-auto max-w-3xl">
    <div className="mb-6">
      <p className="text-xs font-semibold tracking-[.15em] text-[#5268b0]">TÀI KHOẢN</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight text-[#101c43]">Hồ sơ và bảo mật</h1>
      <p className="mt-1 text-sm text-[#8998ae]">Thiết lập OTP và quản lý mật khẩu tài khoản.</p>
    </div>
    {error && <p role="alert" className="mb-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>}
    {message && <p role="status" className="mb-4 rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-800">{message}</p>}
    {loading ? <p className="rounded-2xl bg-white p-6 text-sm text-[#718096]">Đang tải hồ sơ...</p> : profile && <>
      <div className="rounded-2xl bg-white p-5 sm:p-6">
        <h2 className="text-lg font-semibold text-[#17264a]">Thông tin tài khoản</h2>
        <p className="mt-1 text-sm text-[#718096]">Email tài khoản không thể thay đổi.</p>
        <form onSubmit={submitProfileUpdate} className="mt-5 grid gap-4 sm:grid-cols-2">
          <div className="space-y-2"><Label htmlFor="profile-full-name">Họ và tên</Label><Input id="profile-full-name" required maxLength={150} value={profile.fullName} onChange={event => setProfile(current => current ? { ...current, fullName: event.target.value } : current)} /></div>
          <div className="space-y-2"><Label htmlFor="profile-avatar-url">Ảnh đại diện (URL)</Label><Input id="profile-avatar-url" type="url" maxLength={2048} value={profile.avatarUrl ?? ''} onChange={event => setProfile(current => current ? { ...current, avatarUrl: event.target.value } : current)} placeholder="https://..." /></div>
          <div className="sm:col-span-2"><p className="text-xs text-[#8998ae]">Email tài khoản: <span className="font-medium text-[#52627c]">{profile.email}</span></p></div>
          <div className="sm:col-span-2"><Button type="submit" disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu thông tin cá nhân'}</Button></div>
        </form>
      </div>

      <div className="mt-5 rounded-2xl bg-white p-5 sm:p-6">
        <div className="flex items-start gap-3">
          <span className={`grid size-10 shrink-0 place-items-center rounded-xl ${profile.isOtpConfigured ? 'bg-green-50 text-green-700' : 'bg-amber-50 text-amber-700'}`}>
            {profile.isOtpConfigured ? <ShieldCheck className="size-5" /> : <AlertTriangle className="size-5" />}
          </span>
          <div>
            <h2 className="font-semibold text-[#17264a]">Xác thực OTP</h2>
            <p className="mt-1 text-sm leading-6 text-[#718096]">
              {profile.isOtpConfigured
                ? 'OTP đã được thiết lập. Mỗi mã chỉ xác nhận được một lần cho mỗi chu kỳ.'
                : 'Tài khoản chưa thiết lập OTP. Hãy bật OTP trong ứng dụng Authenticator để bảo vệ thao tác đổi mật khẩu.'}
            </p>
          </div>
        </div>
        {!profile.isOtpConfigured && !setup && <form onSubmit={beginSetup} className="mt-5 max-w-md space-y-3">
          <div className="space-y-2"><Label htmlFor="otp-setup-email">Nhập lại email tài khoản</Label><Input id="otp-setup-email" type="email" autoComplete="email" required value={setupEmail} onChange={event => setSetupEmail(event.target.value)} /></div>
          <Button type="submit" disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang tạo...' : 'Thiết lập OTP'}</Button>
        </form>}
        {!profile.isOtpConfigured && setup && <div className="mt-5 space-y-5">
          <div className="rounded-xl border border-[#e2e8f4] bg-[#f8faff] p-4">
            <p className="text-sm font-medium text-[#243d9a]">Thêm CRM vào ứng dụng Authenticator</p>
            <p className="mt-2 text-xs leading-5 text-[#718096]">Trong Google Authenticator, nhấn dấu + → “Nhập khóa thiết lập”. Dán khóa bên dưới, chọn loại khóa dựa trên thời gian (Time based), rồi nhập mã 6 số ứng dụng tạo ra để xác nhận. Không nhập URI vào ô khóa.</p>
            <Label htmlFor="otp-secret" className="mt-4 block">Khóa thiết lập</Label>
            <Input id="otp-secret" readOnly value={setup.secret} className="mt-1 bg-white font-mono tracking-widest" />
            <Label htmlFor="otp-uri" className="mt-3 block">URI provisioning</Label>
            <textarea id="otp-uri" readOnly value={setup.provisioningUri} className="mt-1 min-h-20 w-full break-all rounded-lg border border-input bg-white p-3 font-mono text-xs" />
          </div>
          <form onSubmit={confirmSetup} className="max-w-md space-y-3">
            <div className="space-y-2"><Label htmlFor="otp-setup-code">Nhập mã 6 số từ ứng dụng</Label><Input id="otp-setup-code" inputMode="numeric" pattern="[0-9]{6}" maxLength={6} autoComplete="one-time-code" required value={setupCode} onChange={event => setSetupCode(event.target.value.replace(/\D/g, '').slice(0, 6))} /></div>
            <div className="flex gap-2"><Button type="submit" disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang xác nhận...' : 'Xác nhận và bật OTP'}</Button><Button type="button" variant="outline" disabled={saving} onClick={() => { setSetup(null); setSetupCode('') }}>Hủy</Button></div>
          </form>
        </div>}
      </div>

      <div className="mt-5 rounded-2xl bg-white p-5 sm:p-6">
        <div className="flex items-center gap-3"><span className="grid size-10 place-items-center rounded-xl bg-[#eef4ff] text-[#5268b0]"><KeyRound className="size-5" /></span><div><h2 className="font-semibold text-[#17264a]">Đổi mật khẩu</h2><p className="mt-1 text-sm text-[#718096]">Yêu cầu nhập đúng email tài khoản và mã OTP mới nhất.</p></div></div>
        {!profile.isOtpConfigured ? <p className="mt-4 rounded-xl bg-amber-50 p-3 text-sm text-amber-800">Thiết lập OTP phía trên để mở khóa đổi mật khẩu.</p> : <form onSubmit={submitPasswordChange} className="mt-5 grid gap-4 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2"><Label htmlFor="change-email">Nhập lại email tài khoản</Label><Input id="change-email" type="email" autoComplete="email" required value={email} onChange={event => setEmail(event.target.value)} /></div>
          <div className="space-y-2 sm:col-span-2"><Label htmlFor="change-otp">Mã OTP 6 số</Label><Input id="change-otp" inputMode="numeric" pattern="[0-9]{6}" maxLength={6} autoComplete="one-time-code" required value={changeCode} onChange={event => setChangeCode(event.target.value.replace(/\D/g, '').slice(0, 6))} /></div>
          <div className="space-y-2"><Label htmlFor="new-password">Mật khẩu mới</Label><Input id="new-password" type="password" autoComplete="new-password" minLength={12} maxLength={1024} required value={newPassword} onChange={event => setNewPassword(event.target.value)} /></div>
          <div className="space-y-2"><Label htmlFor="confirm-new-password">Xác nhận mật khẩu mới</Label><Input id="confirm-new-password" type="password" autoComplete="new-password" minLength={12} maxLength={1024} required value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} /></div>
          <div className="sm:col-span-2"><Button type="submit" disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang đổi...' : 'Đổi mật khẩu'}</Button></div>
        </form>}
      </div>
    </>}
  </section>
}

export default ProfilePage
